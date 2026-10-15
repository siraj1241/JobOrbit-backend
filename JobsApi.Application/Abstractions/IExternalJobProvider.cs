using JobsApi.Application.Dtos;

namespace JobsApi.Application.Abstractions;

/// <summary>
/// Abstraction over an external job-board provider (Arbeitnow, TheMuse, Himalayas, Adzuna).
/// Each implementation is a typed HttpClient registered in DI.
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

/// <summary>
/// Composite provider that aggregates results from all registered
/// IExternalJobProvider implementations in parallel and merges them.
/// </summary>
public interface IExternalJobAggregator
{
    /// <summary>Fetch jobs from ALL registered providers and merge by ExternalId.</summary>
    Task<IReadOnlyList<ExternalJobDto>> FetchAllAsync(ExternalJobsQueryDto query, CancellationToken ct);
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
    DateTime PublishedAt,
    string? Source = null);
