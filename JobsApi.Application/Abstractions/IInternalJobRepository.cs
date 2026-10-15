using JobsApi.Domain.Entities;

namespace JobsApi.Application.Abstractions;

public interface IInternalJobRepository
{
    Task<InternalJob?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<InternalJob>> GetByEmployerAsync(Guid employerId, CancellationToken ct);
    Task<(IReadOnlyList<InternalJob> Items, int Total)> SearchPublishedAsync(
        string? search, string? location, bool? remote, string? tag,
        int page, int pageSize, CancellationToken ct);

    Task AddAsync(InternalJob job, CancellationToken ct);
    Task UpdateAsync(InternalJob job, CancellationToken ct);
    Task DeleteAsync(InternalJob job, CancellationToken ct);
}
