namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

/// <summary>
/// Contains metadata describing a Prometheus metric.
/// </summary>
/// <param name="Type">The metric type.</param>
/// <param name="Help">The help text.</param>
public sealed record PrometheusMetricMetadata(string? Type, string? Help);
