using Bravellian.InfraMonitor.Metrics;

namespace Bravellian.InfraMonitor.Metrics.AspNetCore;

public sealed class BravellianMetricsOptions
{
    public BravellianMeterOptions Meter { get; set; } = new();
    public bool EnableAspNetCoreInstrumentation { get; set; } = true;
    public bool EnableRuntimeInstrumentation { get; set; } = true;
    public bool EnableProcessInstrumentation { get; set; } = true;
    public bool EnablePrometheusExporter { get; set; } = false;
    public string PrometheusEndpointPath { get; set; } = "/metrics";
    public int PrometheusScrapeResponseCacheMilliseconds { get; set; } = 300;
}
