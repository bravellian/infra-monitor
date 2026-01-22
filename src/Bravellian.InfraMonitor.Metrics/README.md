# Bravellian.InfraMonitor.Metrics

Core metrics primitives used by Bravellian Infra Monitor. Provides a meter
provider and helpers for counters, histograms, and gauges.

## Usage

Create a meter provider and use it to create metrics.

```csharp
var provider = new BravellianMeterProvider("MyApp", "1.0.0");

var requests = provider.CreateCounter("myapp.requests", "requests", "Total requests.");
requests.Add(1);

var duration = provider.CreateHistogram("myapp.request.duration", "ms", "Request duration.");
duration.Record(12.5);

var queueDepth = provider.CreateObservableGauge("myapp.queue.depth", () => 42, "items", "Current queue depth.");
```

## Recipes

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
