using System.Collections.Generic;

namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

/// <summary>
/// Represents a parsed Prometheus metric sample.
/// </summary>
/// <param name="Name">The metric name.</param>
/// <param name="Labels">The label set associated with the sample.</param>
/// <param name="Value">The raw sample value.</param>
/// <param name="Timestamp">The optional sample timestamp.</param>
public sealed record PrometheusMetricSample(
    string Name,
    IReadOnlyDictionary<string, string> Labels,
    string Value,
    long? Timestamp);
