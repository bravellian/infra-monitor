using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bravellian.InfraMonitor.Metrics.Ui.Models;

namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

/// <summary>
/// Scrapes Prometheus endpoints and parses their responses.
/// </summary>
public sealed class MetricsScrapeService
{
    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes the scraper with an HTTP client.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to fetch metrics.</param>
    public MetricsScrapeService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    /// <summary>
    /// Scrapes a registered endpoint and returns the parsed report.
    /// </summary>
    /// <param name="registration">The metrics endpoint registration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The scrape report.</returns>
    public async Task<MetricsEndpointReport> ScrapeAsync(
        MetricsEndpointRegistration registration,
        CancellationToken cancellationToken)
    {
        var start = DateTimeOffset.UtcNow;
        try
        {
            using var response = await httpClient.GetAsync(registration.EndpointUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = PrometheusMetricsParser.Parse(payload);
            var duration = DateTimeOffset.UtcNow - start;
            return new MetricsEndpointReport(registration, snapshot, null, duration);
        }
        catch (Exception ex)
        {
            var duration = DateTimeOffset.UtcNow - start;
            return new MetricsEndpointReport(registration, null, ex.ToString(), duration);
        }
    }
}
