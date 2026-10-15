using JobsApi.Application.Abstractions;
using JobsApi.Domain.Entities;
using JobsApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobsApi.Infrastructure.Repositories;

public class InternalJobRepository : IInternalJobRepository
{
    private readonly JobsDbContext _db;

    public InternalJobRepository(JobsDbContext db) => _db = db;

    public Task<InternalJob?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.InternalJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task<IReadOnlyList<InternalJob>> GetByEmployerAsync(
        Guid employerId, CancellationToken ct)
    {
        return await _db.InternalJobs
            .Where(j => j.EmployerId == employerId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<InternalJob> Items, int Total)> SearchPublishedAsync(
        string? search, string? location, bool? remote, string? tag,
        int page, int pageSize, CancellationToken ct)
    {
        // Only Published jobs are visible to seekers. Auto-hide if deadline passed.
        var now = DateTime.UtcNow;
        var q = _db.InternalJobs
            .Where(j => j.Status == "Published")
            .Where(j => j.ApplicationDeadline == null || j.ApplicationDeadline > now);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(j =>
                EF.Functions.ILike(j.Title, $"%{term}%")
                || EF.Functions.ILike(j.Description, $"%{term}%"));
        }
        if (!string.IsNullOrWhiteSpace(location))
        {
            var loc = location.Trim();
            q = q.Where(j => EF.Functions.ILike(j.Location, $"%{loc}%"));
        }
        if (remote.HasValue) q = q.Where(j => j.Remote == remote.Value);
        if (!string.IsNullOrWhiteSpace(tag))
        {
            var t = tag.Trim();
            q = q.Where(j => EF.Functions.ILike(j.TagsCsv, $"%{t}%"));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(InternalJob job, CancellationToken ct)
    {
        await _db.InternalJobs.AddAsync(job, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(InternalJob job, CancellationToken ct)
    {
        job.UpdatedAt = DateTime.UtcNow;
        _db.InternalJobs.Update(job);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(InternalJob job, CancellationToken ct)
    {
        _db.InternalJobs.Remove(job);
        await _db.SaveChangesAsync(ct);
    }
}
