using System.Collections.Generic;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Bravellian.InfraMonitor.Metrics.Ui.Services.Setup;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Bravellian.InfraMonitor.Metrics.Ui.Sample;

public sealed class SampleMetricsSetupStore : IMetricsSetupStore
{
    private readonly MetricsUiOptions options;

    public SampleMetricsSetupStore(IOptions<MetricsUiOptions> options)
    {
        this.options = options.Value;
    }

    public bool TryGetMetricsEndpoints(HttpContext context, out IReadOnlyList<MetricsEndpointRegistration> endpoints)
    {
        endpoints = (IReadOnlyList<MetricsEndpointRegistration>)options.Endpoints;
        return endpoints.Count > 0;
    }

    public bool TryGetMetricsRefreshSeconds(HttpContext context, out int refreshSeconds)
    {
        refreshSeconds = options.RefreshSeconds;
        return true;
    }

    public bool TryGetPinnedMetrics(HttpContext context, out IReadOnlyList<string> metricNames)
    {
        metricNames = (IReadOnlyList<string>)options.PinnedMetrics;
        return metricNames.Count > 0;
    }

    public void SetPinnedMetrics(HttpContext context, IReadOnlyList<string> metricNames)
    {
        // No-op for the sample host (read-only config).
    }
}
