# Bravellian.InfraMonitor.Metrics.HttpServer

Standalone HTTP listener that exposes Bravellian Infra Monitor metrics for
Prometheus scraping.

## Usage

Instantiate `BravellianMetricsHttpServer` with the desired options and keep it
alive for the lifetime of your process.
