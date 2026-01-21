using Bravellian.InfraMonitor.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Bravellian.InfraMonitor.Metrics.HttpServer;

/// <summary>
/// Hosts a Prometheus scrape endpoint using the OpenTelemetry HTTP listener.
/// </summary>
public sealed class BravellianMetricsHttpServer : IDisposable
{
    private readonly MeterProvider provider;

    /// <summary>
    /// Initializes the HTTP server using the provided options.
    /// </summary>
    /// <param name="options">The configuration for the server.</param>
    public BravellianMetricsHttpServer(BravellianMetricsHttpServerOptions options)
    {
        provider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(options.Meter.MeterName)
            .ConfigureInstrumentation(options)
            .AddPrometheusHttpListener(listenerOptions =>
            {
                listenerOptions.ScrapeEndpointPath = options.ScrapeEndpointPath;
                listenerOptions.UriPrefixes = options.UriPrefixes;
            })
            .Build();
    }

    /// <summary>
    /// Disposes the underlying OpenTelemetry meter provider.
    /// </summary>
    public void Dispose()
    {
        provider.Dispose();
    }
}
