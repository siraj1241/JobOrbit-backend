using JobsApi.Domain.Entities;

namespace JobsApi.Application.Abstractions;

public interface IResumeRepository
{
    Task<Resume?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task AddAsync(Resume resume, CancellationToken ct);
    Task UpdateAsync(Resume resume, CancellationToken ct);
    Task DeleteAsync(Resume resume, CancellationToken ct);
}
