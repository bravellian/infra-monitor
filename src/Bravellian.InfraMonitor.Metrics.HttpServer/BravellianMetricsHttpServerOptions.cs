using Bravellian.InfraMonitor.Metrics;

namespace Bravellian.InfraMonitor.Metrics.HttpServer;

public sealed class BravellianMetricsHttpServerOptions
{
    public BravellianMeterOptions Meter { get; init; } = new();
    public bool EnableRuntimeInstrumentation { get; init; } = true;
    public bool EnableProcessInstrumentation { get; init; } = true;
    public string[] UriPrefixes { get; init; } = ["http://localhost:9464/"];
    public string ScrapeEndpointPath { get; init; } = "/metrics";
}
