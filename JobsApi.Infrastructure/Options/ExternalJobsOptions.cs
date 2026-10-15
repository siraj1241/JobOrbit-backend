using System.ComponentModel.DataAnnotations;

namespace JobsApi.Infrastructure.Options;

/// <summary>Configuration for the external job-board providers.</summary>
public class ExternalJobsOptions
{
    public const string Section = "ExternalJobs";

    // Arbeitnow
    [Required]
    public string ArbeitnowBaseUrl { get; set; } = "https://www.arbeitnow.com/api/job-board-api";

    // TheMuse
    public string TheMuseApiKey { get; set; } = string.Empty;     // optional — public API works without

    // Adzuna
    public string AdzunaAppId { get; set; } = string.Empty;       // required for Adzuna
    public string AdzunaAppKey { get; set; } = string.Empty;      // required for Adzuna

    // Common HTTP behavior
    [Range(5, 120)]
    public int TimeoutSeconds { get; set; } = 30;

    [Range(0, 5)]
    public int MaxRetries { get; set; } = 3;

    [Range(100, 10000)]
    public int RetryBaseDelayMs { get; set; } = 500;

    [Range(10, 100)]
    public int SyncPageSize { get; set; } = 50;

    // OpenAI (resume builder)
    public string OpenAiApiKey { get; set; } = string.Empty;     // required for AI bullet generation
    public string OpenAiModel { get; set; } = "gpt-4o-mini";
}
