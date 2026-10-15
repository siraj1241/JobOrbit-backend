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
/// Calls the Adzuna jobs API (https://developer.adzuna.com).
/// Requires an App ID and App Key — provided via ExternalJobsOptions.AdzunaAppId
/// and ExternalJobsOptions.AdzunaAppKey.
/// </summary>
public class AdzunaJobProvider : IExternalJobProvider
{
    public string Name => "Adzuna";

    private readonly HttpClient _http;
    private readonly ExternalJobsOptions _opts;
    private readonly ILogger<AdzunaJobProvider> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AdzunaJobProvider(
        HttpClient http,
        IOptions<ExternalJobsOptions> opts,
        ILogger<AdzunaJobProvider> logger)
    {
        _http = http;
        _opts = opts.Value;
        _logger = logger;
        _http.BaseAddress = new Uri("https://api.adzuna.com/v1/api/jobs/");
        _http.Timeout = TimeSpan.FromSeconds(_opts.TimeoutSeconds);
    }

    public async Task<IReadOnlyList<ExternalJobDto>> FetchAsync(
        ExternalJobsQueryDto query,
        CancellationToken ct)
    {
        // Adzuna requires App ID + Key — bail early if not configured.
        if (string.IsNullOrWhiteSpace(_opts.AdzunaAppId) || string.IsNullOrWhiteSpace(_opts.AdzunaAppKey))
        {
            _logger.LogWarning("Adzuna provider called without AppId/ApiKey — returning empty list.");
            return Array.Empty<ExternalJobDto>();
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var country = "us";   // default country; could be parameterized
        var qs = $"{country}/search/{page}?app_id={_opts.AdzunaAppId}" +
                 $"&app_key={_opts.AdzunaAppKey}" +
                 $"&results_per_page={pageSize}";
        if (!string.IsNullOrWhiteSpace(query.Search)) qs += $"&what={WebUtility.UrlEncode(query.Search)}";
        if (!string.IsNullOrWhiteSpace(query.Location)) qs += $"&where={WebUtility.UrlEncode(query.Location)}";
        if (query.Remote.HasValue && query.Remote.Value) qs += "&where=Remote";

        var response = await SendWithRetryAsync(qs, ct);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<AdzunaResponse>(stream, JsonOpts, ct)
                      ?? new AdzunaResponse(Array.Empty<AdzunaJob>(), 0);

        var dtos = payload.Results?.Select(MapToDto).Take(pageSize).ToList() ?? new List<ExternalJobDto>();
        _logger.LogInformation("Adzuna returned {Count} jobs for page {Page}.", payload.Results?.Length ?? 0, page);
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
                    throw new DomainException($"Adzuna returned {(int)resp.StatusCode} for request.");
                }
                last = new HttpRequestException($"Adzuna returned status {(int)resp.StatusCode}.");
                resp.Dispose();
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                last = new TimeoutException("Adzuna request timed out.");
            }
            catch (HttpRequestException ex) { last = ex; }

            if (attempt <= _opts.MaxRetries)
            {
                var delay = _opts.RetryBaseDelayMs * (1 << (attempt - 1));
                _logger.LogWarning("Adzuna attempt {Attempt} failed: {Error}. Retrying in {Delay}ms.",
                    attempt, last?.Message, delay);
                await Task.Delay(delay, ct);
            }
        }
        throw new DomainException(
            $"Failed to reach Adzuna after {_opts.MaxRetries + 1} attempts: {last?.Message}");
    }

    private static ExternalJobDto MapToDto(AdzunaJob j) => new(
        ExternalId: j.Id?.ToString() ?? Guid.NewGuid().ToString(),
        Title: j.Title ?? "Untitled",
        CompanyName: j.Company?.DisplayName ?? "Unknown",
        Description: j.Description ?? string.Empty,
        Location: FormatLocation(j.Location),
        Remote: (j.Location?.Area?.Any(a => a.Equals("Remote", StringComparison.OrdinalIgnoreCase)) == true),
        Tags: Array.Empty<string>().ToList(),
        JobTypes: !string.IsNullOrEmpty(j.ContractTime) ? new[] { j.ContractTime } : Array.Empty<string>(),
        Url: j.RedirectUrl ?? string.Empty,
        PublishedAt: ParseDate(j.Created),
        Source: "Adzuna");

    private static string FormatLocation(Location? loc)
    {
        if (loc?.Area == null || loc.Area.Length == 0) return "Unknown";
        return string.Join(", ", loc.Area);
    }

    private static DateTime ParseDate(string? iso) =>
        DateTime.TryParse(iso, out var d) ? d : DateTime.UtcNow;

    // POCOs matching Adzuna JSON contract
    private sealed class AdzunaResponse
    {
        public AdzunaJob[]? Results { get; set; }
        public int Count { get; set; }
    }

    private sealed class AdzunaJob
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? RedirectUrl { get; set; }
        public string? Created { get; set; }
        public string? ContractTime { get; set; }
        public Company? Company { get; set; }
        public Location? Location { get; set; }
    }

    private sealed class Company { public string? DisplayName { get; set; } }
    private sealed class Location { public string[]? Area { get; set; } }
}
