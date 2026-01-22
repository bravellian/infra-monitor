# Bravellian.InfraMonitor.Metrics.Ui

Razor Pages UI for Bravellian Infra Monitor metrics. Provides a simple
dashboard that scrapes Prometheus endpoints and renders common charts.

## Usage

- Register the UI services via `AddBravellianMetricsUi(...)`.
- Configure endpoints to scrape in your setup store.

```csharp
builder.Services.AddBravellianMetricsUi();
```

The UI is exposed under the `/metrics-report` Razor Pages route.

Configuration example
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

Wire a setup store (see the sample host for a complete runnable setup):
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
