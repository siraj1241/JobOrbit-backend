using JobsApi.Application.Abstractions;
using JobsApi.Application.Dtos;
using JobsApi.Domain.Entities;
using JobsApi.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace JobsApi.Infrastructure.Jobs;

public class InternalJobService : IInternalJobService
{
    private readonly IInternalJobRepository _jobs;
    private readonly IUserRepository _users;
    private readonly ILogger<InternalJobService> _logger;

    private static readonly HashSet<string> ValidStatuses = new() { "Draft", "Published", "Closed" };
    private static readonly HashSet<string> ValidEmploymentTypes = new()
    { "Full-time", "Part-time", "Contract", "Internship" };

    public InternalJobService(
        IInternalJobRepository jobs,
        IUserRepository users,
        ILogger<InternalJobService> logger)
    {
        _jobs = jobs;
        _users = users;
        _logger = logger;
    }

    public async Task<JobPostingDto> CreateAsync(
        Guid employerId, CreateJobPostingDto dto, CancellationToken ct)
    {
        ValidateDto(dto);

        var employer = await _users.GetByIdAsync(employerId, ct)
            ?? throw new UnauthorizedException("Employer not found.");

        var job = new InternalJob
        {
            EmployerId = employerId,
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Location = dto.Location.Trim(),
            Remote = dto.Remote,
            EmploymentType = dto.EmploymentType,
            SalaryMin = dto.SalaryMin,
            SalaryMax = dto.SalaryMax,
            SalaryCurrency = string.IsNullOrWhiteSpace(dto.SalaryCurrency) ? null : dto.SalaryCurrency.Trim().ToUpperInvariant(),
            TagsCsv = dto.TagsCsv ?? string.Empty,
            ApplicationUrl = dto.ApplicationUrl,
            ApplicationInstructions = dto.ApplicationInstructions,
            ApplicationDeadline = dto.ApplicationDeadline,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _jobs.AddAsync(job, ct);

        _logger.LogInformation("Employer {EmployerId} created job {JobId} ({Status}).",
            employerId, job.Id, job.Status);

        return MapToDto(job, employer.DisplayName);
    }

    public async Task<JobPostingDto> UpdateAsync(
        Guid employerId, Guid jobId, CreateJobPostingDto dto, CancellationToken ct)
    {
        ValidateDto(dto);

        var job = await _jobs.GetByIdAsync(jobId, ct)
            ?? throw new NotFoundException("InternalJob", jobId);

        if (job.EmployerId != employerId)
            throw new UnauthorizedException("You do not own this job posting.");

        var employer = await _users.GetByIdAsync(employerId, ct);

        job.Title = dto.Title.Trim();
        job.Description = dto.Description.Trim();
        job.Location = dto.Location.Trim();
        job.Remote = dto.Remote;
        job.EmploymentType = dto.EmploymentType;
        job.SalaryMin = dto.SalaryMin;
        job.SalaryMax = dto.SalaryMax;
        job.SalaryCurrency = string.IsNullOrWhiteSpace(dto.SalaryCurrency) ? null : dto.SalaryCurrency.Trim().ToUpperInvariant();
        job.TagsCsv = dto.TagsCsv ?? string.Empty;
        job.ApplicationUrl = dto.ApplicationUrl;
        job.ApplicationInstructions = dto.ApplicationInstructions;
        job.ApplicationDeadline = dto.ApplicationDeadline;
        job.Status = dto.Status;

        await _jobs.UpdateAsync(job, ct);

        _logger.LogInformation("Employer {EmployerId} updated job {JobId}.", employerId, jobId);

        return MapToDto(job, employer?.DisplayName ?? "");
    }

    public async Task DeleteAsync(Guid employerId, Guid jobId, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(jobId, ct)
            ?? throw new NotFoundException("InternalJob", jobId);

        if (job.EmployerId != employerId)
            throw new UnauthorizedException("You do not own this job posting.");

        await _jobs.DeleteAsync(job, ct);
        _logger.LogInformation("Employer {EmployerId} deleted job {JobId}.", employerId, jobId);
    }

    public async Task<IReadOnlyList<JobPostingDto>> ListByEmployerAsync(
        Guid employerId, CancellationToken ct)
    {
        var jobs = await _jobs.GetByEmployerAsync(employerId, ct);
        var employer = await _users.GetByIdAsync(employerId, ct);
        return jobs.Select(j => MapToDto(j, employer?.DisplayName ?? "")).ToList();
    }

    public async Task<JobPostingDto?> GetByIdForEmployerAsync(
        Guid employerId, Guid jobId, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(jobId, ct);
        if (job is null || job.EmployerId != employerId) return null;
        var employer = await _users.GetByIdAsync(employerId, ct);
        return MapToDto(job, employer?.DisplayName ?? "");
    }

    // ─── Helpers ──────────────────────────────────────────

    private static void ValidateDto(CreateJobPostingDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new DomainException("Title is required.");
        if (dto.Title.Length > 300)
            throw new DomainException("Title must not exceed 300 characters.");
        if (string.IsNullOrWhiteSpace(dto.Description))
            throw new DomainException("Description is required.");
        if (string.IsNullOrWhiteSpace(dto.Location))
            throw new DomainException("Location is required.");
        if (!ValidEmploymentTypes.Contains(dto.EmploymentType))
            throw new DomainException($"EmploymentType must be one of: {string.Join(", ", ValidEmploymentTypes)}.");
        if (!ValidStatuses.Contains(dto.Status))
            throw new DomainException($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
        if (dto.SalaryMin.HasValue && dto.SalaryMax.HasValue && dto.SalaryMin > dto.SalaryMax)
            throw new DomainException("SalaryMin cannot exceed SalaryMax.");
        if (dto.ApplicationDeadline.HasValue && dto.ApplicationDeadline.Value < DateTime.UtcNow)
            throw new DomainException("Application deadline cannot be in the past.");
    }

    private static JobPostingDto MapToDto(InternalJob j, string employerName) => new(
        Id: j.Id,
        Title: j.Title,
        Description: j.Description,
        Location: j.Location,
        Remote: j.Remote,
        EmploymentType: j.EmploymentType,
        SalaryMin: j.SalaryMin,
        SalaryMax: j.SalaryMax,
        SalaryCurrency: j.SalaryCurrency,
        Tags: SplitCsv(j.TagsCsv),
        ApplicationUrl: j.ApplicationUrl,
        ApplicationInstructions: j.ApplicationInstructions,
        ApplicationDeadline: j.ApplicationDeadline,
        Status: j.Status,
        EmployerId: j.EmployerId,
        EmployerName: employerName,
        CreatedAt: j.CreatedAt,
        UpdatedAt: j.UpdatedAt);

    private static IReadOnlyList<string> SplitCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
