# Bravellian Infra Monitor Metrics

Library set for capturing application metrics with OpenTelemetry and exposing them
via Prometheus, plus a Razor Pages UI for browsing metrics snapshots.

Packages
- `Bravellian.InfraMonitor.Metrics` - Core meter provider and instrument helpers.
- `Bravellian.InfraMonitor.Metrics.AspNetCore` - ASP.NET Core registration + Prometheus endpoint.
- `Bravellian.InfraMonitor.Metrics.HttpServer` - Standalone HTTP listener for Prometheus scraping.
- `Bravellian.InfraMonitor.Metrics.Ui` - Razor Pages dashboard that consumes Prometheus endpoints.

Install
```
dotnet add package Bravellian.InfraMonitor.Metrics
dotnet add package Bravellian.InfraMonitor.Metrics.AspNetCore
dotnet add package Bravellian.InfraMonitor.Metrics.HttpServer
dotnet add package Bravellian.InfraMonitor.Metrics.Ui
```

Quick start (ASP.NET Core)
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBravellianMetrics(options =>
{
    options.EnablePrometheusExporter = true;
    options.PrometheusEndpointPath = "/metrics";
});

var app = builder.Build();

app.MapBravellianMetricsEndpoint();
app.Run();
```

Standalone HTTP listener
```csharp
using var server = new BravellianMetricsHttpServer(new BravellianMetricsHttpServerOptions
{
    UriPrefixes = ["http://localhost:9464/"],
    ScrapeEndpointPath = "/metrics"
});

// Keep the process alive while Prometheus scrapes.
```

Custom metrics
```csharp
var provider = new BravellianMeterProvider("MyApp", "1.0.0");
var requests = provider.CreateCounter("myapp.requests", "requests", "Total requests.");
requests.Add(1);
```

Recipes

Counter with labels
```csharp
var provider = new BravellianMeterProvider("MyApp", "1.0.0");
var requests = provider.CreateCounter("myapp.requests", "requests", "Total requests.");

requests.Add(1, new KeyValuePair<string, object?>("route", "/api/orders"));
requests.Add(1, new KeyValuePair<string, object?>("status_code", 200));
```

Histogram with labels
```csharp
var provider = new BravellianMeterProvider("MyApp", "1.0.0");
var latency = provider.CreateHistogram("myapp.http.latency", "ms", "Request latency.");

latency.Record(12.5, new KeyValuePair<string, object?>("route", "/api/orders"));
latency.Record(35.2, new KeyValuePair<string, object?>("route", "/api/orders"));
```

Observable gauge
```csharp
var provider = new BravellianMeterProvider("MyApp", "1.0.0");
_ = provider.CreateObservableGauge("myapp.queue.depth", () => queueDepth, "items", "Queue depth.");
```

UI sample
```csharp
builder.Services.AddBravellianMetricsUi();
```

Visit `/metrics-report` after startup. See
`samples/Bravellian.InfraMonitor.Metrics.Ui.Sample/README.md` for a runnable host.

UI configuration example
```json
{
  "MetricsUi": {
    "RefreshSeconds": 15,
    "Endpoints": [
      {
        "ServiceName": "SampleApp",
        "InstanceName": "Local",
        "EndpointUrl": "http://localhost:5000/metrics"
      }
    ],
    "PinnedMetrics": [
      "process.cpu.time",
      "http.server.request.duration_count"
    ]
  }
}
```

```csharp
builder.Services.Configure<MetricsUiOptions>(builder.Configuration.GetSection("MetricsUi"));
builder.Services.AddSingleton<IMetricsSetupStore, OptionsMetricsSetupStore>();
```

```csharp
public sealed class MetricsUiOptions
{
    public IList<MetricsEndpointRegistration> Endpoints { get; init; } = new List<MetricsEndpointRegistration>();
    public IList<string> PinnedMetrics { get; init; } = new List<string>();
    public int RefreshSeconds { get; init; } = 20;
}

public sealed class OptionsMetricsSetupStore : IMetricsSetupStore
{
    private readonly MetricsUiOptions options;

    public OptionsMetricsSetupStore(IOptions<MetricsUiOptions> options)
    {
        this.options = options.Value;
    }

    public bool TryGetMetricsEndpoints(HttpContext context, out IReadOnlyList<MetricsEndpointRegistration> endpoints)
    {
        endpoints = (IReadOnlyList<MetricsEndpointRegistration>)options.Endpoints;
        return endpoints.Count > 0;
    }

    public bool TryGetMetricsRefreshSeconds(HttpContext context, out int refreshSeconds)
    {
        refreshSeconds = options.RefreshSeconds;
        return true;
    }

    public bool TryGetPinnedMetrics(HttpContext context, out IReadOnlyList<string> metricNames)
    {
        metricNames = (IReadOnlyList<string>)options.PinnedMetrics;
        return metricNames.Count > 0;
    }

    public void SetPinnedMetrics(HttpContext context, IReadOnlyList<string> metricNames)
    {
    }
}
```
