using JobsApi.Application.Abstractions;
using JobsApi.Domain.Entities;
using JobsApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobsApi.Infrastructure.Repositories;

public class JobRepository : IJobRepository
{
    private readonly JobsDbContext _db;

    public JobRepository(JobsDbContext db) => _db = db;

    public Task<Job?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, ct);

    public Task<Job?> GetByExternalIdAsync(string externalId, string source, CancellationToken ct) =>
        _db.Jobs.FirstOrDefaultAsync(
            j => j.ExternalId == externalId && j.Source == source, ct);

    public async Task<(IReadOnlyList<Job> Items, int Total)> SearchAsync(
        string? search,
        string? location,
        bool? remote,
        string? tag,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var q = _db.Jobs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(j =>
                EF.Functions.ILike(j.Title, $"%{term}%")
                || EF.Functions.ILike(j.CompanyName, $"%{term}%")
                || EF.Functions.ILike(j.Description, $"%{term}%"));
        }
        if (!string.IsNullOrWhiteSpace(location))
        {
            var loc = location.Trim();
            q = q.Where(j => EF.Functions.ILike(j.Location, $"%{loc}%"));
        }
        if (remote.HasValue)
        {
            q = q.Where(j => j.Remote == remote.Value);
        }
        if (!string.IsNullOrWhiteSpace(tag))
        {
            var t = tag.Trim();
            q = q.Where(j => EF.Functions.ILike(j.Tags, $"%{t}%"));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(j => j.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task UpsertAsync(Job job, CancellationToken ct)
    {
        var existing = await _db.Jobs.FirstOrDefaultAsync(
            x => x.ExternalId == job.ExternalId && x.Source == job.Source, ct);

        if (existing is null)
        {
            _db.Jobs.Add(job);
        }
        else
        {
            existing.Title = job.Title;
            existing.CompanyName = job.CompanyName;
            existing.Description = job.Description;
            existing.Location = job.Location;
            existing.Remote = job.Remote;
            existing.Tags = job.Tags;
            existing.JobTypes = job.JobTypes;
            existing.Url = job.Url;
            existing.PublishedAt = job.PublishedAt;
            existing.CachedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpsertRangeAsync(IEnumerable<Job> jobs, CancellationToken ct)
    {
        foreach (var job in jobs)
        {
            var existing = await _db.Jobs.FirstOrDefaultAsync(
                x => x.ExternalId == job.ExternalId && x.Source == job.Source, ct);

            if (existing is null)
                _db.Jobs.Add(job);
            else
            {
                existing.Title = job.Title;
                existing.CompanyName = job.CompanyName;
                existing.Description = job.Description;
                existing.Location = job.Location;
                existing.Remote = job.Remote;
                existing.Tags = job.Tags;
                existing.JobTypes = job.JobTypes;
                existing.Url = job.Url;
                existing.PublishedAt = job.PublishedAt;
                existing.CachedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountAsync(CancellationToken ct) =>
        _db.Jobs.CountAsync(ct);
}
