using JobsApi.Application.Abstractions;
using JobsApi.Domain.Entities;
using JobsApi.Infrastructure.Auth;
using JobsApi.Infrastructure.ExternalJobs;
using JobsApi.Infrastructure.Jobs;
using JobsApi.Infrastructure.Options;
using JobsApi.Infrastructure.Persistence;
using JobsApi.Infrastructure.Repositories;
using JobsApi.Infrastructure.Resumes;
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

        // External jobs options
        services.AddOptions<ExternalJobsOptions>()
            .Bind(config.GetSection(ExternalJobsOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Repositories & services (auth)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        // Repositories & services (external jobs)
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IExternalJobAggregator, ExternalJobAggregator>();

        // Each provider is a typed HttpClient
        services.AddHttpClient<IExternalJobProvider, ArbeitnowJobProvider>();
        services.AddHttpClient<IExternalJobProvider, HimalayasJobProvider>();
        services.AddHttpClient<IExternalJobProvider, TheMuseJobProvider>();
        services.AddHttpClient<IExternalJobProvider, AdzunaJobProvider>();

        // Built-in sample jobs (no HTTP, no DB — always available)
        services.AddSingleton<IExternalJobProvider, SampleJobsProvider>();

        // Internal jobs (employer)
        services.AddScoped<IInternalJobRepository, InternalJobRepository>();
        services.AddScoped<IInternalJobService, InternalJobService>();

        // Resumes
        services.AddScoped<IResumeRepository, ResumeRepository>();
        services.AddScoped<IResumeService, ResumeService>();

        return services;
    }
}
