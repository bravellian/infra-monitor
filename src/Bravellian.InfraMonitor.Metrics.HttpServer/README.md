# Bravellian.InfraMonitor.Metrics.HttpServer

Standalone HTTP listener that exposes Bravellian Infra Monitor metrics for
Prometheus scraping.

## Usage

Instantiate `BravellianMetricsHttpServer` with the desired options and keep it
alive for the lifetime of your process.

```csharp
using var server = new BravellianMetricsHttpServer(new BravellianMetricsHttpServerOptions
{
    UriPrefixes = ["http://localhost:9464/"],
    ScrapeEndpointPath = "/metrics",
    EnableRuntimeInstrumentation = true,
    EnableProcessInstrumentation = true,
    Meter = new BravellianMeterOptions
    {
        MeterName = "MyApp",
        MeterVersion = "1.0.0"
    }
});

// Keep the process alive while Prometheus scrapes.
```
