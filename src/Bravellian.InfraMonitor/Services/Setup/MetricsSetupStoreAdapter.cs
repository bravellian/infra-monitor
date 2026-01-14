using System.Collections.Generic;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Bravellian.InfraMonitor.Metrics.Ui.Services.Setup;
using Microsoft.AspNetCore.Http;

namespace Bravellian.InfraMonitor.Services.Setup;

public sealed class MetricsSetupStoreAdapter : IMetricsSetupStore
{
    private readonly ISetupStore setupStore;

    public MetricsSetupStoreAdapter(ISetupStore setupStore)
    {
        this.setupStore = setupStore;
    }

    public bool TryGetMetricsEndpoints(HttpContext context, out IReadOnlyList<MetricsEndpointRegistration> endpoints)
        => setupStore.TryGetMetricsEndpoints(context, out endpoints);

    public bool TryGetMetricsRefreshSeconds(HttpContext context, out int refreshSeconds)
        => setupStore.TryGetMetricsRefreshSeconds(context, out refreshSeconds);

    public bool TryGetPinnedMetrics(HttpContext context, out IReadOnlyList<string> metricNames)
        => setupStore.TryGetPinnedMetrics(context, out metricNames);

    public void SetPinnedMetrics(HttpContext context, IReadOnlyList<string> metricNames)
        => setupStore.SetPinnedMetrics(context, metricNames);
}
