namespace JobsApi.Application.Dtos;

/// <summary>Public-facing job DTO returned by every jobs endpoint.</summary>
public record JobDto(
    Guid Id,
    string ExternalId,
    string Source,
    string Title,
    string CompanyName,
    string Description,
    string Location,
    bool Remote,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> JobTypes,
    string Url,
    DateTime PublishedAt,
    DateTime CachedAt);

/// <summary>Pagination + filter parameters for GET /api/jobs.</summary>
public record JobsQueryDto(
    string? Search,
    string? Location,
    bool? Remote,
    string? Tag,
    int Page = 1,
    int PageSize = 20);

/// <summary>Paginated wrapper returned by list endpoints.</summary>
public record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

/// <summary>Search parameters for the external job board.</summary>
public record ExternalJobsQueryDto(
    string? Search,
    string? Location,
    bool? Remote,
    int Page = 1,
    int PageSize = 20);

/// <summary>Result of a sync operation.</summary>
public record SyncResultDto(
    int Fetched,
    int Added,
    int Updated,
    int Skipped,
    DateTime SyncedAt);
