using JobsApi.Application.Dtos;

namespace JobsApi.Application.Abstractions;

/// <summary>
/// Orchestrates job use-cases: live-fetch from external providers,
/// paginated reads from our local cache, and one-shot sync.
/// </summary>
public interface IJobService
{
    /// <summary>Fetch live jobs from the external provider (does not persist).</summary>
    Task<PagedResultDto<JobDto>> GetExternalAsync(ExternalJobsQueryDto query, CancellationToken ct);

    /// <summary>Search the locally cached jobs with pagination.</summary>
    Task<PagedResultDto<JobDto>> SearchAsync(JobsQueryDto query, CancellationToken ct);

    /// <summary>Get a single cached job by ID.</summary>
    Task<JobDto?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Pull the latest jobs from the provider and upsert into our DB.</summary>
    Task<SyncResultDto> SyncFromExternalAsync(int maxPages, CancellationToken ct);
}
