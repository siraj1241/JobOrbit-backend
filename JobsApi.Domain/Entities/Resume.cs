using System;
using System.Collections.Generic;

namespace JobsApi.Domain.Entities
{
    /// <summary>
    /// A resume owned by a user. Schema follows the JSON Resume standard
    /// (https://jsonresume.org/) but stored relationally.
    /// </summary>
    public class Resume
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }   // → User.Id

        public string FullName { get; set; } = default!;
        public string Headline { get; set; } = default!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
        public string? Summary { get; set; }

        // JSON-serialized lists of ResumeExperience / ResumeEducation
        public string ExperienceJson { get; set; } = "[]";
        public string EducationJson { get; set; } = "[]";
        public string SkillsJson { get; set; } = "[]";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ResumeExperience
    {
        public string Company { get; set; } = default!;
        public string Title { get; set; } = default!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool Current { get; set; }
        public string? Location { get; set; }
        public List<string> BulletPoints { get; set; } = new();
    }

    public class ResumeEducation
    {
        public string Institution { get; set; } = default!;
        public string Degree { get; set; } = default!;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Field { get; set; }
    }
}
