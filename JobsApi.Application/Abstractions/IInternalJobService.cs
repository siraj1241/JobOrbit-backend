using JobsApi.Application.Dtos;

namespace JobsApi.Application.Abstractions;

/// <summary>Employer-side operations for posting &amp; managing internal jobs.</summary>
public interface IInternalJobService
{
    /// <summary>Create a new job posting (Draft or Published). Returns the created DTO.</summary>
    Task<JobPostingDto> CreateAsync(Guid employerId, CreateJobPostingDto dto, CancellationToken ct);

    /// <summary>Update an existing posting. Only the owner can do this.</summary>
    Task<JobPostingDto> UpdateAsync(Guid employerId, Guid jobId, CreateJobPostingDto dto, CancellationToken ct);

    /// <summary>Delete a posting. Only the owner can do this.</summary>
    Task DeleteAsync(Guid employerId, Guid jobId, CancellationToken ct);

    /// <summary>List all postings owned by the given employer.</summary>
    Task<IReadOnlyList<JobPostingDto>> ListByEmployerAsync(Guid employerId, CancellationToken ct);

    /// <summary>Get a single posting by ID (owner only).</summary>
    Task<JobPostingDto?> GetByIdForEmployerAsync(Guid employerId, Guid jobId, CancellationToken ct);
}
