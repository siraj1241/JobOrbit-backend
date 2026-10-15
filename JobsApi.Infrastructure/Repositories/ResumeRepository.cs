using JobsApi.Application.Abstractions;
using JobsApi.Domain.Entities;
using JobsApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobsApi.Infrastructure.Repositories;

public class ResumeRepository : IResumeRepository
{
    private readonly JobsDbContext _db;

    public ResumeRepository(JobsDbContext db) => _db = db;

    public Task<Resume?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        _db.Resumes.FirstOrDefaultAsync(r => r.UserId == userId, ct);

    public async Task AddAsync(Resume resume, CancellationToken ct)
    {
        await _db.Resumes.AddAsync(resume, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Resume resume, CancellationToken ct)
    {
        resume.UpdatedAt = DateTime.UtcNow;
        _db.Resumes.Update(resume);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Resume resume, CancellationToken ct)
    {
        _db.Resumes.Remove(resume);
        await _db.SaveChangesAsync(ct);
    }
}
