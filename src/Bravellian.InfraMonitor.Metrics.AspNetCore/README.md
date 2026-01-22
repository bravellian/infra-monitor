# Bravellian.InfraMonitor.Metrics.AspNetCore

ASP.NET Core integration for Bravellian Infra Monitor metrics. Registers
OpenTelemetry metrics, instrumentation, and an optional Prometheus endpoint.

## Usage

- Call `AddBravellianMetrics(...)` during service registration.
- Map the endpoint via `MapBravellianMetricsEndpoint()` or
  `UseBravellianMetricsEndpoint()`.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBravellianMetrics(options =>
{
    options.EnablePrometheusExporter = true;
    options.EnableAspNetCoreInstrumentation = true;
    options.EnableRuntimeInstrumentation = true;
    options.EnableProcessInstrumentation = true;
    options.Meter = new BravellianMeterOptions
    {
        MeterName = "MyApp",
        MeterVersion = "1.0.0"
    };
});

var app = builder.Build();
app.MapBravellianMetricsEndpoint();
app.Run();
```
