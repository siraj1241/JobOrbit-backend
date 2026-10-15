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
/// Calls TheMuse public jobs API (https://www.themuse.com/developers/api/v2).
/// Public API key is optional but recommended; we accept it via options.
/// Endpoint: https://www.themuse.com/api/public/jobs?&lt;page&gt;
/// </summary>
public class TheMuseJobProvider : IExternalJobProvider
{
    public string Name => "TheMuse";

    private readonly HttpClient _http;
    private readonly ExternalJobsOptions _opts;
    private readonly ILogger<TheMuseJobProvider> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TheMuseJobProvider(
        HttpClient http,
        IOptions<ExternalJobsOptions> opts,
        ILogger<TheMuseJobProvider> logger)
    {
        _http = http;
        _opts = opts.Value;
        _logger = logger;
        _http.BaseAddress = new Uri("https://www.themuse.com/api/public/");
        _http.Timeout = TimeSpan.FromSeconds(_opts.TimeoutSeconds);
    }

    public async Task<IReadOnlyList<ExternalJobDto>> FetchAsync(
        ExternalJobsQueryDto query,
        CancellationToken ct)
    {
        var page = Math.Max(0, query.Page - 1);    // TheMuse is 0-indexed
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var qs = $"jobs?page={page}";
        if (!string.IsNullOrWhiteSpace(query.Search))
            qs += $"&category={WebUtility.UrlEncode(query.Search)}";

        var response = await SendWithRetryAsync(qs, ct);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<TheMuseResponse>(stream, JsonOpts, ct)
                      ?? new TheMuseResponse(Array.Empty<TheMuseJob>(), 0);

        IEnumerable<TheMuseJob> filtered = payload.Jobs ?? Array.Empty<TheMuseJob>();
        if (query.Remote.HasValue) filtered = filtered.Where(j => j.Locations?.Any(l => l.Name == "Remote") == query.Remote.Value);
        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var loc = query.Location.Trim();
            filtered = filtered.Where(j => j.Locations?.Any(l => (l.Name ?? "").Contains(loc, StringComparison.OrdinalIgnoreCase)) == true);
        }

        var dtos = filtered.Select(MapToDto).Take(pageSize).ToList();
        _logger.LogInformation("TheMuse returned {Count} jobs for page {Page} (filtered to {Returned}).",
            payload.Jobs?.Length ?? 0, page, dtos.Count);
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
                    throw new DomainException($"TheMuse returned {(int)resp.StatusCode} for request.");
                }
                last = new HttpRequestException($"TheMuse returned status {(int)resp.StatusCode}.");
                resp.Dispose();
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                last = new TimeoutException("TheMuse request timed out.");
            }
            catch (HttpRequestException ex) { last = ex; }

            if (attempt <= _opts.MaxRetries)
            {
                var delay = _opts.RetryBaseDelayMs * (1 << (attempt - 1));
                _logger.LogWarning("TheMuse attempt {Attempt} failed: {Error}. Retrying in {Delay}ms.",
                    attempt, last?.Message, delay);
                await Task.Delay(delay, ct);
            }
        }
        throw new DomainException(
            $"Failed to reach TheMuse after {_opts.MaxRetries + 1} attempts: {last?.Message}");
    }

    private static ExternalJobDto MapToDto(TheMuseJob j) => new(
        ExternalId: j.Id?.ToString() ?? Guid.NewGuid().ToString(),
        Title: j.Name ?? "Untitled",
        CompanyName: j.Company?.Name ?? "Unknown",
        Description: j.Contents ?? string.Empty,
        Location: j.Locations?.FirstOrDefault()?.Name ?? "Unknown",
        Remote: j.Locations?.Any(l => (l.Name ?? "").Equals("Remote", StringComparison.OrdinalIgnoreCase)) == true,
        Tags: j.Levels?.Select(l => l.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>(),
        JobTypes: Array.Empty<string>().ToList(),
        Url: j.Ref ?? string.Empty,
        PublishedAt: ParseDate(j.PublicationDate),
        Source: "TheMuse");

    private static DateTime ParseDate(string? iso) =>
        DateTime.TryParse(iso, out var d) ? d : DateTime.UtcNow;

    // POCOs matching TheMuse JSON contract
    private sealed record TheMuseResponse(TheMuseJob[]? Jobs, int Page, long PageCount);

    private sealed class TheMuseJob
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Contents { get; set; }
        public string? Ref { get; set; }
        public string? PublicationDate { get; set; }
        public Company? Company { get; set; }
        public Location[]? Locations { get; set; }
        public Level[]? Levels { get; set; }
    }

    private sealed class Company { public string? Name { get; set; } }
    private sealed class Location { public string? Name { get; set; } }
    private sealed class Level { public string? Name { get; set; } }
}
