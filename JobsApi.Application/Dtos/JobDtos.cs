namespace JobsApi.Application.Dtos;

/// <summary>Public-facing job DTO returned by every jobs endpoint.</summary>
public record JobDto(
    Guid Id,
    string ExternalId,
    string Source,            // "Arbeitnow" | "TheMuse" | "Himalayas" | "Adzuna" | "Internal"
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
    string? Search = null,
    string? Location = null,
    bool? Remote = null,
    int Page = 1,
    int PageSize = 20);

/// <summary>Result of a sync operation.</summary>
public record SyncResultDto(
    int Fetched,
    int Added,
    int Updated,
    int Skipped,
    DateTime SyncedAt);

// ───────────────────────────────────────────────────────────────────
//  Internal (employer-posted) jobs
// ───────────────────────────────────────────────────────────────────

/// <summary>Body of POST/PUT /api/employer/jobs. Created/updated by employers only.</summary>
public record CreateJobPostingDto(
    string Title,
    string Description,
    string Location,
    bool Remote,
    string EmploymentType,        // Full-time | Part-time | Contract | Internship
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,       // ISO 4217: USD, EUR, INR...
    string? TagsCsv,              // "react,typescript,aws"
    string? ApplicationUrl,       // where the seeker applies
    string? ApplicationInstructions,
    DateTime? ApplicationDeadline,
    string Status);               // "Draft" | "Published" | "Closed"

/// <summary>An employer-posted job as seen by the API consumer.</summary>
public record JobPostingDto(
    Guid Id,
    string Title,
    string Description,
    string Location,
    bool Remote,
    string EmploymentType,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,
    IReadOnlyList<string> Tags,
    string? ApplicationUrl,
    string? ApplicationInstructions,
    DateTime? ApplicationDeadline,
    string Status,                 // Draft | Published | Closed
    Guid EmployerId,
    string EmployerName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ───────────────────────────────────────────────────────────────────
//  Resume builder
// ───────────────────────────────────────────────────────────────────

/// <summary>A resume entry created/owned by a user.</summary>
public record ResumeDto(
    Guid Id,
    string FullName,
    string Headline,                  // e.g. "Senior Frontend Engineer"
    string? Email,
    string? Phone,
    string? Location,
    string? Summary,                  // 2-3 sentence professional summary
    IReadOnlyList<ResumeExperienceDto> Experience,
    IReadOnlyList<ResumeEducationDto> Education,
    IReadOnlyList<string> Skills,
    DateTime UpdatedAt);

public record ResumeExperienceDto(
    string Company,
    string Title,
    DateTime StartDate,
    DateTime? EndDate,
    bool Current,
    string? Location,
    IReadOnlyList<string> BulletPoints);

public record ResumeEducationDto(
    string Institution,
    string Degree,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Field);

/// <summary>Body of POST/PUT /api/resume.</summary>
public record UpsertResumeDto(
    string FullName,
    string Headline,
    string? Email,
    string? Phone,
    string? Location,
    string? Summary,
    IReadOnlyList<ResumeExperienceDto> Experience,
    IReadOnlyList<ResumeEducationDto> Education,
    IReadOnlyList<string> Skills);

/// <summary>Request body for the AI bullet-point generator.</summary>
public record GenerateBulletsRequestDto(
    string Company,
    string Title,
    string? ExistingSummary,
    string? JobDescription);        // optional, for tailoring

/// <summary>AI-generated resume bullet suggestions.</summary>
public record GeneratedBulletsDto(
    string Company,
    string Title,
    IReadOnlyList<string> BulletPoints,
    string? SummarySuggestion,
    DateTime GeneratedAt);
