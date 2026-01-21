# Bravellian.InfraMonitor.Metrics.AspNetCore

ASP.NET Core integration for Bravellian Infra Monitor metrics. Registers
OpenTelemetry metrics, instrumentation, and an optional Prometheus endpoint.

## Usage

- Call `AddBravellianMetrics(...)` during service registration.
- Map the endpoint via `MapBravellianMetricsEndpoint()` or
  `UseBravellianMetricsEndpoint()`.
