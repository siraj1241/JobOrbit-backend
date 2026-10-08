// src/JobsApi.Infrastructure/Persistence/DbSeeder.cs
using JobsApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobsApi.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedTestUserAsync(this IServiceProvider sp, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<JobsDbContext>();
        var hasher = provider.GetRequiredService<IPasswordHasher<User>>();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DbSeeder");   // ✅ non-generic logger

        try
        {
            await db.Database.MigrateAsync(ct);

            if (await db.Users.AnyAsync(u => u.Email == "test@joborbit.com", ct))
            {
                logger.LogDebug("Test user already exists — skipping seed.");
                return;
            }

            var user = new User
            {
                Email = "test@joborbit.com",
                DisplayName = "Test User",
                Role = "User"
            };
            user.PasswordHash = hasher.HashPassword(user, "Password@123");

            db.Users.Add(user);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Seeded test user test@joborbit.com / Password@123");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}