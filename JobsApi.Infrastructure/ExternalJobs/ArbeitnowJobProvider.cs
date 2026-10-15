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
/// Calls the Arbeitnow public job-board API (no auth required).
/// Documentation: https://www.arbeitnow.com/api/job-board-api
/// </summary>
public class ArbeitnowJobProvider : IExternalJobProvider
{
    public string Name => "Arbeitnow";

    private readonly HttpClient _http;
    private readonly ExternalJobsOptions _opts;
    private readonly ILogger<ArbeitnowJobProvider> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public ArbeitnowJobProvider(
        HttpClient http,
        IOptions<ExternalJobsOptions> opts,
        ILogger<ArbeitnowJobProvider> logger)
    {
        _http = http;
        _opts = opts.Value;
        _logger = logger;
        _http.BaseAddress = new Uri(_opts.ArbeitnowBaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(_opts.TimeoutSeconds);
    }

    public async Task<IReadOnlyList<ExternalJobDto>> FetchAsync(
        ExternalJobsQueryDto query,
        CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        // Arbeitnow does not natively support search/keyword filtering —
        // we fetch a page and filter client-side when filters are provided.
        var response = await SendWithRetryAsync($"?page={page}", ct);
        var payload = await ParseAsync(response, ct);

        var jobs = payload.Jobs ?? Array.Empty<ArbeitnowJob>();

        IEnumerable<ArbeitnowJob> filtered = jobs;
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            filtered = filtered.Where(j =>
                (j.Title?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (j.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (j.CompanyName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var loc = query.Location.Trim();
            filtered = filtered.Where(j =>
                j.Location?.Contains(loc, StringComparison.OrdinalIgnoreCase) ?? false);
        }
        if (query.Remote.HasValue)
        {
            filtered = filtered.Where(j => j.Remote == query.Remote.Value);
        }

        var dtos = filtered
            .Select(MapToDto)
            .Take(pageSize)
            .ToList();

        _logger.LogInformation(
            "Arbeitnow returned {Total} jobs for page {Page} (filtered to {Returned}).",
            jobs.Length, page, dtos.Count);

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

                if (resp.StatusCode == HttpStatusCode.NotFound
                    || resp.StatusCode == HttpStatusCode.BadRequest)
                {
                    resp.Dispose();
                    throw new DomainException($"Arbeitnow returned {(int)resp.StatusCode} for request.");
                }

                last = new HttpRequestException($"Arbeitnow returned status {(int)resp.StatusCode}.");
                resp.Dispose();
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                last = new TimeoutException("Arbeitnow request timed out.");
            }
            catch (HttpRequestException ex)
            {
                last = ex;
            }

            if (attempt <= _opts.MaxRetries)
            {
                var delay = _opts.RetryBaseDelayMs * (1 << (attempt - 1));
                _logger.LogWarning(
                    "Arbeitnow attempt {Attempt} failed: {Error}. Retrying in {Delay}ms.",
                    attempt, last?.Message, delay);
                await Task.Delay(delay, ct);
            }
        }

        throw new DomainException(
            $"Failed to reach external job provider after {_opts.MaxRetries + 1} attempts: {last?.Message}");
    }

    private static async Task<ArbeitnowResponse> ParseAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<ArbeitnowResponse>(stream, JsonOpts, ct);
        return payload ?? new ArbeitnowResponse(Array.Empty<ArbeitnowJob>());
    }

    private static ExternalJobDto MapToDto(ArbeitnowJob j) => new(
        ExternalId: j.Id?.ToString() ?? j.Slug ?? Guid.NewGuid().ToString(),
        Title: j.Title ?? "Untitled",
        CompanyName: j.CompanyName ?? "Unknown",
        Description: j.Description ?? string.Empty,
        Location: j.Location ?? "Unknown",
        Remote: j.Remote,
        Tags: (j.Tags ?? Array.Empty<string>()).ToList(),
        JobTypes: (j.JobTypes ?? Array.Empty<string>()).ToList(),
        Url: j.Url ?? string.Empty,
        PublishedAt: ParseEpoch(j.CreatedAt),
        Source: "Arbeitnow");

    /// <summary>Arbeitnow returns created_at as a Unix epoch (seconds since 1970).</summary>
    private static DateTime ParseEpoch(long? epoch) =>
        epoch.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(epoch.Value).UtcDateTime
            : DateTime.UtcNow;

    private static DateTime ParseDate(string? iso)
        => DateTime.TryParse(iso, out var d) ? d : DateTime.UtcNow;

    // --- POCOs matching the Arbeitnow JSON contract ---
    // Top-level response: { "data": [...jobs], "links": {...}, "meta": {...} }
    // We only need the "data" array. The Count field is for our fallback only.
    private sealed record ArbeitnowResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("data")]
        ArbeitnowJob[]? Jobs)
    {
        public ArbeitnowResponse() : this(null) { }
        public ArbeitnowResponse(ArbeitnowJob[] jobs) : this(jobs) { }
    }

    private sealed record ArbeitnowJob
    {
        public int? Id { get; init; }
        public string? Slug { get; init; }
        public string? Title { get; init; }
        public string? CompanyName { get; init; }
        public string? Description { get; init; }
        public string? Location { get; init; }
        public bool Remote { get; init; }
        public string? Url { get; init; }
        public string[]? Tags { get; init; }
        public string[]? JobTypes { get; init; }

        // Arbeitnow returns created_at as a Unix epoch integer (e.g. 1792081863),
        // not an ISO 8601 string. Use a JsonNumberHandling that allows conversion
        // OR read it as a long and convert manually.
        public long? CreatedAt { get; init; }
    }
}
