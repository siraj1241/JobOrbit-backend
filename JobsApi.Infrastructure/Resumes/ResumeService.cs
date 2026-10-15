using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JobsApi.Application.Abstractions;
using JobsApi.Application.Dtos;
using JobsApi.Domain.Entities;
using JobsApi.Domain.Exceptions;
using JobsApi.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobsApi.Infrastructure.Resumes;

/// <summary>
/// Resume service that persists user resumes AND calls OpenAI to generate
/// bullet points / professional summary suggestions on demand.
///
/// If OpenAiApiKey is not configured, the AI generator endpoint returns a
/// helpful error explaining how to configure it.
/// </summary>
public class ResumeService : IResumeService
{
    private readonly IResumeRepository _resumes;
    private readonly IUserRepository _users;
    private readonly ExternalJobsOptions _opts;
    private readonly HttpClient _http;          // OpenAI client (no typed client needed)
    private readonly ILogger<ResumeService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ResumeService(
        IResumeRepository resumes,
        IUserRepository users,
        IOptions<ExternalJobsOptions> opts,
        ILogger<ResumeService> logger)
    {
        _resumes = resumes;
        _users = users;
        _opts = opts.Value;
        _logger = logger;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public async Task<ResumeDto?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var r = await _resumes.GetByUserIdAsync(userId, ct);
        return r is null ? null : MapToDto(r);
    }

    public async Task<ResumeDto> UpsertAsync(Guid userId, UpsertResumeDto dto, CancellationToken ct)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new DomainException("FullName is required.");
        if (string.IsNullOrWhiteSpace(dto.Headline))
            throw new DomainException("Headline is required.");

        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new UnauthorizedException("User not found.");

        var existing = await _resumes.GetByUserIdAsync(userId, ct);
        var resume = existing ?? new Resume { UserId = userId };

        resume.FullName = dto.FullName.Trim();
        resume.Headline = dto.Headline.Trim();
        resume.Email = dto.Email?.Trim();
        resume.Phone = dto.Phone?.Trim();
        resume.Location = dto.Location?.Trim();
        resume.Summary = dto.Summary?.Trim();
        resume.ExperienceJson = JsonSerializer.Serialize(dto.Experience ?? new List<ResumeExperienceDto>(), JsonOpts);
        resume.EducationJson = JsonSerializer.Serialize(dto.Education ?? new List<ResumeEducationDto>(), JsonOpts);
        resume.SkillsJson = JsonSerializer.Serialize(dto.Skills ?? new List<string>(), JsonOpts);

        if (existing is null) await _resumes.AddAsync(resume, ct);
        else await _resumes.UpdateAsync(resume, ct);

        _logger.LogInformation("Resume upserted for user {UserId}.", userId);
        return MapToDto(resume);
    }

    public async Task DeleteAsync(Guid userId, CancellationToken ct)
    {
        var resume = await _resumes.GetByUserIdAsync(userId, ct)
            ?? throw new NotFoundException("Resume", userId);
        await _resumes.DeleteAsync(resume, ct);
    }

    public async Task<GeneratedBulletsDto> GenerateBulletsAsync(
        Guid userId, GenerateBulletsRequestDto req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opts.OpenAiApiKey))
        {
            throw new DomainException(
                "OpenAI API key is not configured. Set ExternalJobs:OpenAiApiKey in appsettings " +
                "or the OPENAI_API_KEY environment variable. Get a key at https://platform.openai.com/api-keys.");
        }

        if (string.IsNullOrWhiteSpace(req.Company) || string.IsNullOrWhiteSpace(req.Title))
            throw new DomainException("Company and Title are required for bullet generation.");

        var prompt = BuildPrompt(req);

        var requestBody = new
        {
            model = _opts.OpenAiModel,
            messages = new[]
            {
                new { role = "system", content = "You are an expert resume writer. Generate concise, achievement-focused resume bullet points in JSON format only. Do not include any prose outside JSON." },
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            response_format = new { type = "json_object" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opts.OpenAiApiKey);

        var response = await _http.SendAsync(request, ct);
        var responseText = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenAI returned {Status}: {Body}", (int)response.StatusCode, responseText);
            throw new DomainException($"OpenAI request failed ({(int)response.StatusCode}).");
        }

        // Parse the OpenAI response and extract the JSON content
        using var doc = JsonDocument.Parse(responseText);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        // The model returns { "bullets": [...], "summary": "..." }
        using var inner = JsonDocument.Parse(content);
        var bullets = inner.RootElement.GetProperty("bullets")
            .EnumerateArray()
            .Select(b => b.GetString() ?? "")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        var summary = inner.RootElement.TryGetProperty("summary", out var s) ? s.GetString() : null;

        _logger.LogInformation("Generated {Count} bullet points for {Company}/{Title}.",
            bullets.Count, req.Company, req.Title);

        return new GeneratedBulletsDto(
            Company: req.Company,
            Title: req.Title,
            BulletPoints: bullets,
            SummarySuggestion: summary,
            GeneratedAt: DateTime.UtcNow);
    }

    // ─── Helpers ──────────────────────────────────────────

    private static string BuildPrompt(GenerateBulletsRequestDto req)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Generate 5 achievement-focused resume bullet points for the following role:");
        sb.AppendLine($"Company: {req.Company}");
        sb.AppendLine($"Title: {req.Title}");
        if (!string.IsNullOrWhiteSpace(req.ExistingSummary))
            sb.AppendLine($"Existing summary: {req.ExistingSummary}");
        if (!string.IsNullOrWhiteSpace(req.JobDescription))
            sb.AppendLine($"Job description to tailor to:\n{req.JobDescription}");
        sb.AppendLine();
        sb.AppendLine("Each bullet should start with a strong action verb and include a measurable outcome where possible.");
        sb.AppendLine();
        sb.AppendLine("Return ONLY a JSON object with this exact shape:");
        sb.AppendLine(@"{ ""bullets"": [""...""], ""summary"": ""a 2-sentence professional summary"" }");
        return sb.ToString();
    }

    private static ResumeDto MapToDto(Resume r) => new(
        Id: r.Id,
        FullName: r.FullName,
        Headline: r.Headline,
        Email: r.Email,
        Phone: r.Phone,
        Location: r.Location,
        Summary: r.Summary,
        Experience: JsonSerializer.Deserialize<List<ResumeExperienceDto>>(r.ExperienceJson, JsonOpts) ?? new(),
        Education: JsonSerializer.Deserialize<List<ResumeEducationDto>>(r.EducationJson, JsonOpts) ?? new(),
        Skills: JsonSerializer.Deserialize<List<string>>(r.SkillsJson, JsonOpts) ?? new(),
        UpdatedAt: r.UpdatedAt);
}
