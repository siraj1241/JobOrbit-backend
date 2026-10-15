using System;
using System.Collections.Generic;

namespace JobsApi.Domain.Entities
{
    /// <summary>
    /// A job posted directly by an employer in our portal (internal jobs).
    /// Distinct from the cached external jobs (Arbeitnow etc.) which live in
    /// the `Job` entity. Internal jobs support full lifecycle (draft/publish/closed).
    /// </summary>
    public class InternalJob
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid EmployerId { get; set; }       // → User.Id (role=Employer)

        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Location { get; set; } = default!;
        public bool Remote { get; set; }

        /// <summary>Full-time | Part-time | Contract | Internship</summary>
        public string EmploymentType { get; set; } = "Full-time";

        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string? SalaryCurrency { get; set; }   // ISO 4217

        /// <summary>Comma-separated tags: "react,typescript,aws"</summary>
        public string TagsCsv { get; set; } = string.Empty;

        public string? ApplicationUrl { get; set; }
        public string? ApplicationInstructions { get; set; }
        public DateTime? ApplicationDeadline { get; set; }

        /// <summary>Draft | Published | Closed</summary>
        public string Status { get; set; } = "Draft";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
