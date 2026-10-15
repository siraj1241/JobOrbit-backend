using System;
using System.Collections.Generic;

namespace JobsApi.Domain.Entities
{
    /// <summary>
    /// Represents a job listing sourced from an external job board
    /// (e.g. Arbeitnow) and cached locally for search / filtering.
    /// </summary>
    public class Job
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>The external provider's identifier (e.g. Arbeitnow job id).</summary>
        public string ExternalId { get; set; } = default!;

        /// <summary>Which provider the job came from — "Arbeitnow", "TheMuse", etc.</summary>
        public string Source { get; set; } = "Arbeitnow";

        public string Title { get; set; } = default!;
        public string CompanyName { get; set; } = default!;
        public string Description { get; set; } = default!;

        /// <summary>Where the job is located (e.g. "Berlin", "Remote").</summary>
        public string Location { get; set; } = default!;

        /// <summary>True if the position is fully remote.</summary>
        public bool Remote { get; set; }

        /// <summary>Comma-separated tags / skills (e.g. "python,django,aws").</summary>
        public string Tags { get; set; } = string.Empty;

        /// <summary>Comma-separated job types (e.g. "full_time,contract").</summary>
        public string JobTypes { get; set; } = string.Empty;

        /// <summary>Original listing URL on the provider's site.</summary>
        public string Url { get; set; } = default!;

        /// <summary>When the job was originally published by the provider.</summary>
        public DateTime PublishedAt { get; set; }

        /// <summary>When we cached/last refreshed this job in our DB.</summary>
        public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    }
}
