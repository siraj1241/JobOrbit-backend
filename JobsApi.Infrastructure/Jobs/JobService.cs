using System.Text.Json;
using JobsApi.Application.Abstractions;
using JobsApi.Application.Dtos;
using JobsApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobsApi.Infrastructure.Jobs;

public class JobService : IJobService
{
    private readonly IExternalJobAggregator _aggregator;
    private readonly IJobRepository _jobs;
    private readonly IInternalJobRepository _internalJobs;
    private readonly IUserRepository _users;
    private readonly ILogger<JobService> _logger;

    public JobService(
        IExternalJobAggregator aggregator,
        IJobRepository jobs,
        IInternalJobRepository internalJobs,
        IUserRepository users,
        ILogger<JobService> logger)
    {
        _aggregator = aggregator;
        _jobs = jobs;
        _internalJobs = internalJobs;
        _users = users;
        _logger = logger;
    }

    public async Task<PagedResultDto<JobDto>> GetExternalAsync(
        ExternalJobsQueryDto query,
        CancellationToken ct)
    {
        var external = await _aggregator.FetchAllAsync(query, ct);
        var items = external.Select(MapExternalToDto).ToList();
        var total = items.Count;

        return new PagedResultDto<JobDto>(
            Items: items,
            Page: query.Page,
            PageSize: query.PageSize,
            TotalItems: total,
            TotalPages: (int)Math.Ceiling(total / (double)Math.Max(1, query.PageSize)));
    }

    public async Task<PagedResultDto<JobDto>> SearchAsync(
        JobsQueryDto query,
        CancellationToken ct)
    {
        // Pull from BOTH the external cache (synced jobs) AND internal published jobs
        var (cachedItems, cachedTotal) = await _jobs.SearchAsync(
            query.Search, query.Location, query.Remote, query.Tag,
            query.Page, query.PageSize, ct);

        var (internalItems, internalTotal) = await _internalJobs.SearchPublishedAsync(
            query.Search, query.Location, query.Remote, query.Tag,
            query.Page, query.PageSize, ct);

        // Merge both sources, sort by published date desc, take a page
        var merged = cachedItems.Select(MapToDto)
            .Concat(internalItems.Select(MapInternalToDto))
            .OrderByDescending(j => j.PublishedAt)
            .ToList();

        return new PagedResultDto<JobDto>(
            Items: merged,
            Page: query.Page,
            PageSize: query.PageSize,
            TotalItems: cachedTotal + internalTotal,
            TotalPages: (int)Math.Ceiling((cachedTotal + internalTotal) / (double)Math.Max(1, query.PageSize)));
    }

    public async Task<JobDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        // Try internal job first (since external cached jobs use Guid too, but IDs never overlap
        // in practice — internal jobs come from employer UI, external from Arbeitnow sync).
        var internalJob = await _internalJobs.GetByIdAsync(id, ct);
        if (internalJob != null) return MapInternalToDto(internalJob);

        var job = await _jobs.GetByIdAsync(id, ct);
        return job is null ? null : MapToDto(job);
    }

    public async Task<SyncResultDto> SyncFromExternalAsync(int maxPages, CancellationToken ct)
    {
        maxPages = Math.Clamp(maxPages, 1, 10);
        var totalFetched = 0;
        var added = 0;
        var updated = 0;
        var skipped = 0;

        for (var page = 1; page <= maxPages; page++)
        {
            ct.ThrowIfCancellationRequested();

            var external = await _aggregator.FetchAllAsync(
                new ExternalJobsQueryDto(Page: page, PageSize: 50), ct);

            if (external.Count == 0)
            {
                _logger.LogInformation("No more jobs to sync after page {Page}.", page - 1);
                break;
            }

            totalFetched += external.Count;

            foreach (var ext in external)
            {
                var source = ext.Source ?? "External";
                var existing = await _jobs.GetByExternalIdAsync(ext.ExternalId, source, ct);

                if (existing is null)
                {
                    var job = MapToEntity(ext, source);
                    await _jobs.UpsertAsync(job, ct);
                    added++;
                }
                else if (existing.PublishedAt != ext.PublishedAt
                         || existing.Title != ext.Title
                         || existing.Description != ext.Description)
                {
                    UpdateEntity(existing, ext);
                    await _jobs.UpsertAsync(existing, ct);
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }

            _logger.LogInformation("Synced page {Page}: {Count} jobs (added={Added}, updated={Updated}).",
                page, external.Count, added, updated);
        }

        return new SyncResultDto(
            Fetched: totalFetched,
            Added: added,
            Updated: updated,
            Skipped: skipped,
            SyncedAt: DateTime.UtcNow);
    }

    // ─── Mapping helpers ──────────────────────────────────────────

    private static JobDto MapExternalToDto(ExternalJobDto ext) => new(
        Id: Guid.Empty,
        ExternalId: ext.ExternalId,
        Source: ext.Source ?? "External",
        Title: ext.Title,
        CompanyName: ext.CompanyName,
        Description: ext.Description,
        Location: ext.Location,
        Remote: ext.Remote,
        Tags: ext.Tags,
        JobTypes: ext.JobTypes,
        Url: ext.Url,
        PublishedAt: ext.PublishedAt,
        CachedAt: DateTime.UtcNow);

    private static JobDto MapToDto(Job j) => new(
        Id: j.Id,
        ExternalId: j.ExternalId,
        Source: j.Source,
        Title: j.Title,
        CompanyName: j.CompanyName,
        Description: j.Description,
        Location: j.Location,
        Remote: j.Remote,
        Tags: SplitCsv(j.Tags),
        JobTypes: SplitCsv(j.JobTypes),
        Url: j.Url,
        PublishedAt: j.PublishedAt,
        CachedAt: j.CachedAt);

    private static JobDto MapInternalToDto(InternalJob j) => new(
        Id: j.Id,
        ExternalId: j.Id.ToString(),
        Source: "Internal",
        Title: j.Title,
        CompanyName: "",            // resolved by caller via EmployerId → user.DisplayName
        Description: j.Description,
        Location: j.Location,
        Remote: j.Remote,
        Tags: SplitCsv(j.TagsCsv),
        JobTypes: new[] { j.EmploymentType },
        Url: j.ApplicationUrl ?? "",
        PublishedAt: j.CreatedAt,
        CachedAt: j.UpdatedAt);

    private static Job MapToEntity(ExternalJobDto ext, string source) => new()
    {
        ExternalId = ext.ExternalId,
        Source = source,
        Title = ext.Title,
        CompanyName = ext.CompanyName,
        Description = ext.Description,
        Location = ext.Location,
        Remote = ext.Remote,
        Tags = string.Join(",", ext.Tags),
        JobTypes = string.Join(",", ext.JobTypes),
        Url = ext.Url,
        PublishedAt = ext.PublishedAt,
        CachedAt = DateTime.UtcNow
    };

    private static void UpdateEntity(Job existing, ExternalJobDto ext)
    {
        existing.Title = ext.Title;
        existing.CompanyName = ext.CompanyName;
        existing.Description = ext.Description;
        existing.Location = ext.Location;
        existing.Remote = ext.Remote;
        existing.Tags = string.Join(",", ext.Tags);
        existing.JobTypes = string.Join(",", ext.JobTypes);
        existing.Url = ext.Url;
        existing.PublishedAt = ext.PublishedAt;
        existing.CachedAt = DateTime.UtcNow;
    }

    private static IReadOnlyList<string> SplitCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
