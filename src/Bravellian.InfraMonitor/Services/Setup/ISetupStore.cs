using System;
using System.Collections.Generic;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Microsoft.AspNetCore.Http;

namespace Bravellian.InfraMonitor.Services.Setup;

public interface ISetupStore
{
    SetupStorageMode Mode { get; }
    bool TryGetPostmarkToken(HttpContext context, out string token);
    bool TryGetSqlConnectionString(HttpContext context, out string connectionString);
    bool TryGetMetricsEndpoints(HttpContext context, out IReadOnlyList<MetricsEndpointRegistration> endpoints);
    bool TryGetMetricsRefreshSeconds(HttpContext context, out int refreshSeconds);
    bool TryGetPinnedMetrics(HttpContext context, out IReadOnlyList<string> metricNames);
    bool TryGetPostmarkTokenForServer(out string token);
    bool TryGetSqlConnectionStringForServer(out string connectionString);
    bool TryGetMetricsEndpointsForServer(out IReadOnlyList<MetricsEndpointRegistration> endpoints);
    bool TryGetMetricsRefreshSecondsForServer(out int refreshSeconds);
    bool TryGetPinnedMetricsForServer(out IReadOnlyList<string> metricNames);
    void SetPostmarkToken(HttpContext context, string token, TimeSpan? lifetime = null);
    void SetSqlConnectionString(HttpContext context, string connectionString, TimeSpan? lifetime = null);
    void SetMetricsEndpoints(HttpContext context, IReadOnlyList<MetricsEndpointRegistration> endpoints, TimeSpan? lifetime = null);
    void SetMetricsRefreshSeconds(HttpContext context, int refreshSeconds, TimeSpan? lifetime = null);
    void SetPinnedMetrics(HttpContext context, IReadOnlyList<string> metricNames, TimeSpan? lifetime = null);
    void ClearPostmarkToken(HttpContext context);
    void ClearSqlConnectionString(HttpContext context);
    void ClearMetricsEndpoints(HttpContext context);
    void ClearMetricsRefreshSeconds(HttpContext context);
    void ClearPinnedMetrics(HttpContext context);
}
