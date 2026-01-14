using Bravellian.InfraMonitor.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Bravellian.InfraMonitor.Metrics.HttpServer;

public sealed class BravellianMetricsHttpServer : IDisposable
{
    private readonly MeterProvider provider;

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

    public void Dispose()
    {
        provider.Dispose();
    }
}
