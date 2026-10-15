using System.Net;
using System.Text.Json;
using JobsApi.Application.Abstractions;
using JobsApi.Application.Dtos;
using JobsApi.Domain.Exceptions;
using JobsApi.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobsApi.Infrastructure.ExternalJobs;

/// <summary>
/// Calls the Himalayas remote-jobs public API (no auth required).
/// Endpoint: https:// Himalayas on Apify — but the simpler documented one is:
///   https://himalayas.app/api/jobs  — returns a JSON array of job listings.
/// </summary>
public class HimalayasJobProvider : IExternalJobProvider
{
    public string Name => "Himalayas";

    private readonly HttpClient _http;
    private readonly ExternalJobsOptions _opts;
    private readonly ILogger<HimalayasJobProvider> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HimalayasJobProvider(
        HttpClient http,
        IOptions<ExternalJobsOptions> opts,
        ILogger<HimalayasJobProvider> logger)
    {
        _http = http;
        _opts = opts.Value;
        _logger = logger;
        _http.BaseAddress = new Uri("https://himalayas.app/api/");
        _http.Timeout = TimeSpan.FromSeconds(_opts.TimeoutSeconds);
    }

    public async Task<IReadOnlyList<ExternalJobDto>> FetchAsync(
        ExternalJobsQueryDto query,
        CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        // Himalayas supports `?search=` and `?limit=` query params.
        var qs = $"jobs?limit={pageSize}";
        if (!string.IsNullOrWhiteSpace(query.Search)) qs += $"&search={WebUtility.UrlEncode(query.Search)}";

        var response = await SendWithRetryAsync(qs, ct);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var jobs = await JsonSerializer.DeserializeAsync<List<HimalayasJob>>(stream, JsonOpts, ct)
                   ?? new List<HimalayasJob>();

        IEnumerable<HimalayasJob> filtered = jobs;
        if (query.Remote.HasValue) filtered = filtered.Where(j => j.IsRemote == query.Remote.Value);
        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var loc = query.Location.Trim();
            filtered = filtered.Where(j => (j.Location ?? "").Contains(loc, StringComparison.OrdinalIgnoreCase));
        }

        var dtos = filtered.Select(MapToDto).Take(pageSize).ToList();

        _logger.LogInformation(
            "Himalayas returned {Count} jobs for page {Page} (filtered to {Returned}).",
            jobs.Count, page, dtos.Count);

        return dtos;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(string relativeUrl, CancellationToken ct)
    {
        Exception? last = null;
        for (var attempt = 1; attempt <= _opts.MaxRetries + 1; attempt++)
        {
            try
            {
                var resp = await _http.GetAsync(relativeUrl, ct);
                if (resp.IsSuccessStatusCode) return resp;

                if (resp.StatusCode == HttpStatusCode.NotFound || resp.StatusCode == HttpStatusCode.BadRequest)
                {
                    resp.Dispose();
                    throw new DomainException($"Himalayas returned {(int)resp.StatusCode} for request.");
                }
                last = new HttpRequestException($"Himalayas returned status {(int)resp.StatusCode}.");
                resp.Dispose();
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                last = new TimeoutException("Himalayas request timed out.");
            }
            catch (HttpRequestException ex) { last = ex; }

            if (attempt <= _opts.MaxRetries)
            {
                var delay = _opts.RetryBaseDelayMs * (1 << (attempt - 1));
                _logger.LogWarning("Himalayas attempt {Attempt} failed: {Error}. Retrying in {Delay}ms.",
                    attempt, last?.Message, delay);
                await Task.Delay(delay, ct);
            }
        }
        throw new DomainException(
            $"Failed to reach Himalayas after {_opts.MaxRetries + 1} attempts: {last?.Message}");
    }

    private static ExternalJobDto MapToDto(HimalayasJob j) => new(
        ExternalId: j.Id?.ToString() ?? j.Slug ?? Guid.NewGuid().ToString(),
        Title: j.Title ?? "Untitled",
        CompanyName: j.CompanyName ?? j.Company?.Name ?? "Unknown",
        Description: j.Description ?? string.Empty,
        Location: j.Location ?? "Remote",
        Remote: j.IsRemote,
        Tags: (j.Tags ?? Array.Empty<string>()).ToList(),
        JobTypes: !string.IsNullOrEmpty(j.JobType) ? new[] { j.JobType } : Array.Empty<string>(),
        Url: j.Url ?? $"https://himalayas.app/jobs/{j.Slug}",
        PublishedAt: ParseDate(j.PublishedAt),
        Source: "Himalayas");

    private static DateTime ParseDate(string? iso) =>
        DateTime.TryParse(iso, out var d) ? d : DateTime.UtcNow;

    // POCO matching Himalayas JSON contract
    private sealed class HimalayasJob
    {
        public int? Id { get; set; }
        public string? Slug { get; set; }
        public string? Title { get; set; }
        public string? CompanyName { get; set; }
        public Company? Company { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public bool IsRemote { get; set; }
        public string? JobType { get; set; }
        public string? Url { get; set; }
        public string[]? Tags { get; set; }
        public string? PublishedAt { get; set; }
    }

    private sealed class Company
    {
        public string? Name { get; set; }
    }
}
