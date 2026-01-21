using System.Collections.Generic;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Microsoft.AspNetCore.Http;

namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Setup;

/// <summary>
/// Defines storage access for UI metrics setup values.
/// </summary>
public interface IMetricsSetupStore
{
    /// <summary>
    /// Attempts to read configured metrics endpoints.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="endpoints">The resolved endpoints if available.</param>
    /// <returns>True when endpoints are available.</returns>
    bool TryGetMetricsEndpoints(HttpContext context, out IReadOnlyList<MetricsEndpointRegistration> endpoints);

    /// <summary>
    /// Attempts to read the refresh interval for metrics polling.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="refreshSeconds">The refresh interval in seconds.</param>
    /// <returns>True when a refresh interval is available.</returns>
    bool TryGetMetricsRefreshSeconds(HttpContext context, out int refreshSeconds);

    /// <summary>
    /// Attempts to read the list of pinned metrics.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="metricNames">The metric names if available.</param>
    /// <returns>True when pinned metrics are available.</returns>
    bool TryGetPinnedMetrics(HttpContext context, out IReadOnlyList<string> metricNames);

    /// <summary>
    /// Stores the list of pinned metrics.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="metricNames">The metric names to persist.</param>
    void SetPinnedMetrics(HttpContext context, IReadOnlyList<string> metricNames);
}
