using Bravellian.InfraMonitor.Metrics;

namespace Bravellian.InfraMonitor.Metrics.HttpServer;

/// <summary>
/// Configures the self-hosted Prometheus metrics listener.
/// </summary>
public sealed class BravellianMetricsHttpServerOptions
{
    /// <summary>
    /// Gets the meter configuration used by the server.
    /// </summary>
    public BravellianMeterOptions Meter { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether runtime instrumentation is enabled.
    /// </summary>
    public bool EnableRuntimeInstrumentation { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether process instrumentation is enabled.
    /// </summary>
    public bool EnableProcessInstrumentation { get; init; } = true;

    /// <summary>
    /// Gets the URI prefixes to listen on.
    /// </summary>
    public string[] UriPrefixes { get; init; } = ["http://localhost:9464/"];

    /// <summary>
    /// Gets the path where the metrics endpoint is exposed.
    /// </summary>
    public string ScrapeEndpointPath { get; init; } = "/metrics";
}
