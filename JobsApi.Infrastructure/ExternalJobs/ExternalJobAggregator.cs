using JobsApi.Application.Abstractions;
using JobsApi.Application.Dtos;
using Microsoft.Extensions.Logging;

namespace JobsApi.Infrastructure.ExternalJobs;

/// <summary>
/// Composite provider that fans out to all registered IExternalJobProvider
/// implementations, runs them in parallel, and merges results.
///
/// A failure in one provider does not fail the others — partial results
/// are returned with a warning log.
/// </summary>
public class ExternalJobAggregator : IExternalJobAggregator
{
    private readonly IEnumerable<IExternalJobProvider> _providers;
    private readonly ILogger<ExternalJobAggregator> _logger;

    public ExternalJobAggregator(
        IEnumerable<IExternalJobProvider> providers,
        ILogger<ExternalJobAggregator> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExternalJobDto>> FetchAllAsync(
        ExternalJobsQueryDto query,
        CancellationToken ct)
    {
        if (!_providers.Any())
        {
            _logger.LogWarning("No external job providers are registered.");
            return Array.Empty<ExternalJobDto>();
        }

        // Fan out to all providers in parallel
        var tasks = _providers.Select(async p =>
        {
            try
            {
                var results = await p.FetchAsync(query, ct);
                // Stamp each result with its source provider name (LINQ Select
                // produces a new list rather than mutating in place — foreach
                // iteration variables can't be reassigned).
                var stamped = results
                    .Select(r => string.IsNullOrEmpty(r.Source) ? r with { Source = p.Name } : r)
                    .ToList();
                return (Provider: p.Name, Results: (IReadOnlyList<ExternalJobDto>)stamped, Error: (string?)null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed: {Message}", p.Name, ex.Message);
                return (Provider: p.Name, Results: (IReadOnlyList<ExternalJobDto>)Array.Empty<ExternalJobDto>(), Error: ex.Message);
            }
        });

        var batches = await Task.WhenAll(tasks);

        // Merge — deduplicate by (Source + ExternalId)
        var merged = batches
            .SelectMany(b => b.Results)
            .GroupBy(r => $"{r.Source}|{r.ExternalId}")
            .Select(g => g.First())
            .OrderByDescending(r => r.PublishedAt)
            .ToList();

        var failed = batches.Where(b => b.Error != null).Select(b => b.Provider).ToList();
        if (failed.Any())
        {
            _logger.LogWarning("Aggregator completed with {Success}/{Total} providers OK. Failed: {Failed}",
                batches.Length - failed.Count, batches.Length, string.Join(", ", failed));
        }

        _logger.LogInformation("Aggregated {Count} unique jobs from {Providers} providers.",
            merged.Count, _providers.Count());

        return merged;
    }
}
