using System.ComponentModel.DataAnnotations;

namespace JobsApi.Infrastructure.Options;

/// <summary>Configuration for the Arbeitnow external job-board provider.</summary>
public class ExternalJobsOptions
{
    public const string Section = "ExternalJobs";

    /// <summary>Base URL of the Arbeitnow API.</summary>
    [Required]
    public string ArbeitnowBaseUrl { get; set; } = "https://www.arbeitnow.com/api/job-board-api";

    /// <summary>HTTP timeout in seconds.</summary>
    [Range(5, 120)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>How many HTTP retries on transient failures.</summary>
    [Range(0, 5)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>Per-retry delay (milliseconds) — exponential backoff multiplier.</summary>
    [Range(100, 10000)]
    public int RetryBaseDelayMs { get; set; } = 500;

    /// <summary>Default page size when syncing.</summary>
    [Range(10, 100)]
    public int SyncPageSize { get; set; } = 50;
}
