using JobsApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobsApi.Infrastructure.Persistence;

public class JobsDbContext : DbContext
{
    public JobsDbContext(DbContextOptions<JobsDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Job> Jobs => Set<Job>();                    // external cached jobs
    public DbSet<InternalJob> InternalJobs => Set<InternalJob>();
    public DbSet<Resume> Resumes => Set<Resume>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
            e.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Role).HasMaxLength(50).HasDefaultValue("User");
            e.Property(u => u.CompanyName).HasMaxLength(200);
        });

        b.Entity<Job>(e =>
        {
            e.HasKey(j => j.Id);
            e.HasIndex(j => new { j.ExternalId, j.Source }).IsUnique();
            e.HasIndex(j => j.PublishedAt);
            e.HasIndex(j => j.Remote);
            e.Property(j => j.ExternalId).HasMaxLength(128).IsRequired();
            e.Property(j => j.Source).HasMaxLength(50).IsRequired();
            e.Property(j => j.Title).HasMaxLength(300).IsRequired();
            e.Property(j => j.CompanyName).HasMaxLength(200).IsRequired();
            e.Property(j => j.Description).HasColumnType("text").IsRequired();
            e.Property(j => j.Location).HasMaxLength(200).IsRequired();
            e.Property(j => j.Tags).HasMaxLength(500).HasDefaultValue("");
            e.Property(j => j.JobTypes).HasMaxLength(200).HasDefaultValue("");
            e.Property(j => j.Url).HasMaxLength(1000).IsRequired();
        });

        b.Entity<InternalJob>(e =>
        {
            e.HasKey(j => j.Id);
            e.HasIndex(j => j.EmployerId);
            e.HasIndex(j => j.Status);
            e.HasIndex(j => new { j.Status, j.ApplicationDeadline });
            e.Property(j => j.Title).HasMaxLength(300).IsRequired();
            e.Property(j => j.Description).HasColumnType("text").IsRequired();
            e.Property(j => j.Location).HasMaxLength(200).IsRequired();
            e.Property(j => j.EmploymentType).HasMaxLength(50).IsRequired();
            e.Property(j => j.SalaryCurrency).HasMaxLength(3);             // ISO 4217
            e.Property(j => j.SalaryMin).HasColumnType("numeric(12,2)");
            e.Property(j => j.SalaryMax).HasColumnType("numeric(12,2)");
            e.Property(j => j.TagsCsv).HasMaxLength(500).HasDefaultValue("");
            e.Property(j => j.ApplicationUrl).HasMaxLength(1000);
            e.Property(j => j.ApplicationInstructions).HasMaxLength(2000);
            e.Property(j => j.Status).HasMaxLength(20).HasDefaultValue("Draft");

            // FK to User (Employer)
            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(j => j.EmployerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Resume>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.UserId).IsUnique();            // one resume per user (for now)
            e.Property(r => r.FullName).HasMaxLength(200).IsRequired();
            e.Property(r => r.Headline).HasMaxLength(200).IsRequired();
            e.Property(r => r.Email).HasMaxLength(256);
            e.Property(r => r.Phone).HasMaxLength(40);
            e.Property(r => r.Location).HasMaxLength(200);
            e.Property(r => r.Summary).HasMaxLength(2000);
            e.Property(r => r.ExperienceJson).HasColumnType("jsonb").IsRequired();
            e.Property(r => r.EducationJson).HasColumnType("jsonb").IsRequired();
            e.Property(r => r.SkillsJson).HasColumnType("jsonb").IsRequired();

            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}