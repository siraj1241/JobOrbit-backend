using JobsApi.Application.Abstractions;
using JobsApi.Application.Dtos;
using JobsApi.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JobsApi.Infrastructure.Jobs;

public class JobService : IJobService
{
    private readonly IExternalJobProvider _provider;
    private readonly IJobRepository _jobs;
    private readonly ILogger<JobService> _logger;

    public JobService(
        IExternalJobProvider provider,
        IJobRepository jobs,
        ILogger<JobService> logger)
    {
        _provider = provider;
        _jobs = jobs;
        _logger = logger;
    }

    public async Task<PagedResultDto<JobDto>> GetExternalAsync(
        ExternalJobsQueryDto query,
        CancellationToken ct)
    {
        var external = await _provider.FetchAsync(query, ct);
        var items = external.Select(MapToDto).ToList();
        var total = items.Count;

        return new PagedResultDto<JobDto>(
            items,
            query.Page,
            query.PageSize,
            total,
            totalPages: (int)Math.Ceiling(total / (double)Math.Max(1, query.PageSize)));
    }

    public async Task<PagedResultDto<JobDto>> SearchAsync(
        JobsQueryDto query,
        CancellationToken ct)
    {
        var (items, total) = await _jobs.SearchAsync(
            query.Search,
            query.Location,
            query.Remote,
            query.Tag,
            query.Page,
            query.PageSize,
            ct);

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResultDto<JobDto>(
            dtos,
            query.Page,
            query.PageSize,
            total,
            totalPages: (int)Math.Ceiling(total / (double)Math.Max(1, query.PageSize)));
    }

    public async Task<JobDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
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

            var external = await _provider.FetchAsync(
                new ExternalJobsQueryDto(Page: page, PageSize: 50), ct);

            if (external.Count == 0)
            {
                _logger.LogInformation("No more jobs to sync after page {Page}.", page - 1);
                break;
            }

            totalFetched += external.Count;

            foreach (var ext in external)
            {
                var existing = await _jobs.GetByExternalIdAsync(ext.ExternalId, _provider.Name, ct);

                if (existing is null)
                {
                    var job = MapToEntity(ext, _provider.Name);
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

            _logger.LogInformation(
                "Synced page {Page}: {Count} jobs (added={Added}, updated={Updated}).",
                page, external.Count, added, updated);
        }

        return new SyncResultDto(
            Fetched: totalFetched,
            Added: added,
            Updated: updated,
            Skipped: skipped,
            SyncedAt: DateTime.UtcNow);
    }

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

    private static IReadOnlyList<string> SplitCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
