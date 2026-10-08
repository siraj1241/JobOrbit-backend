using JobsApi.Application.Abstractions;
using JobsApi.Domain.Entities;
using JobsApi.Infrastructure.Auth;
using JobsApi.Infrastructure.Options;
using JobsApi.Infrastructure.Persistence;
using JobsApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobsApi.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobsApiInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<JobsDbContext>(opt =>
            opt.UseNpgsql(config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string missing.")));

        services.AddOptions<JwtOptions>()
            .Bind(config.GetSection(JwtOptions.Section))
            .ValidateDataAnnotations()
            .Validate(o => o.Secret.Length >= 32, "JWT secret must be at least 32 characters")
            .ValidateOnStart();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        return services;
    }
}