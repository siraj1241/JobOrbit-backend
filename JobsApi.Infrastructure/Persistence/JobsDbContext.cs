using JobsApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobsApi.Infrastructure.Persistence;

public class JobsDbContext : DbContext
{
    public JobsDbContext(DbContextOptions<JobsDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Job> Jobs => Set<Job>();

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
    }
}