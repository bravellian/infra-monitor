using System.Collections.Generic;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Microsoft.AspNetCore.Http;

namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Setup;

public interface IMetricsSetupStore
{
    bool TryGetMetricsEndpoints(HttpContext context, out IReadOnlyList<MetricsEndpointRegistration> endpoints);
    bool TryGetMetricsRefreshSeconds(HttpContext context, out int refreshSeconds);
    bool TryGetPinnedMetrics(HttpContext context, out IReadOnlyList<string> metricNames);
    void SetPinnedMetrics(HttpContext context, IReadOnlyList<string> metricNames);
}
