using JobsApi.Application.Dtos;

namespace JobsApi.Application.Abstractions;

/// <summary>
/// Abstraction over an external job-board provider (Arbeitnow, TheMuse, etc.).
/// Implementations live in the Infrastructure layer and call real HTTP APIs.
/// </summary>
public interface IExternalJobProvider
{
    /// <summary>Which provider this is — e.g. "Arbeitnow".</summary>
    string Name { get; }

    /// <summary>Fetch a page of jobs from the external board.</summary>
    Task<IReadOnlyList<ExternalJobDto>> FetchAsync(
        ExternalJobsQueryDto query,
        CancellationToken ct);
}

/// <summary>Provider-agnostic job DTO returned by external providers.</summary>
public record ExternalJobDto(
    string ExternalId,
    string Title,
    string CompanyName,
    string Description,
    string Location,
    bool Remote,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> JobTypes,
    string Url,
    DateTime PublishedAt);
