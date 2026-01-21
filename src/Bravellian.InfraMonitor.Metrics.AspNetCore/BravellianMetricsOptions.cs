using Bravellian.InfraMonitor.Metrics;

namespace Bravellian.InfraMonitor.Metrics.AspNetCore;

/// <summary>
/// Configures instrumentation and Prometheus exposure for ASP.NET Core.
/// </summary>
public sealed class BravellianMetricsOptions
{
    /// <summary>
    /// Gets or sets options for the underlying meter.
    /// </summary>
    public BravellianMeterOptions Meter { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether ASP.NET Core instrumentation is enabled.
    /// </summary>
    public bool EnableAspNetCoreInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether runtime instrumentation is enabled.
    /// </summary>
    public bool EnableRuntimeInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether process instrumentation is enabled.
    /// </summary>
    public bool EnableProcessInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the Prometheus exporter is enabled.
    /// </summary>
    public bool EnablePrometheusExporter { get; set; } = false;

    /// <summary>
    /// Gets or sets the path for the Prometheus scrape endpoint.
    /// </summary>
    public string PrometheusEndpointPath { get; set; } = "/metrics";

    /// <summary>
    /// Gets or sets the cache duration (milliseconds) for scrape responses.
    /// </summary>
    public int PrometheusScrapeResponseCacheMilliseconds { get; set; } = 300;
}
