# Bravellian.InfraMonitor.Metrics.Ui

Razor Pages UI for Bravellian Infra Monitor metrics. Provides a simple
dashboard that scrapes Prometheus endpoints and renders common charts.

## Usage

- Register the UI services via `AddBravellianMetricsUi(...)`.
- Configure endpoints to scrape in your setup store.
