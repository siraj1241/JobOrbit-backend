using JobsApi.Domain.Entities;

namespace JobsApi.Application.Abstractions;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Job?> GetByExternalIdAsync(string externalId, string source, CancellationToken ct);
    Task<(IReadOnlyList<Job> Items, int Total)> SearchAsync(
        string? search,
        string? location,
        bool? remote,
        string? tag,
        int page,
        int pageSize,
        CancellationToken ct);

    Task UpsertAsync(Job job, CancellationToken ct);
    Task UpsertRangeAsync(IEnumerable<Job> jobs, CancellationToken ct);
    Task<int> CountAsync(CancellationToken ct);
}
