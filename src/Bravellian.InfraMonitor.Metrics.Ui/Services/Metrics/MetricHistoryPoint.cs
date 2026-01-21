namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

/// <summary>
/// Represents a single historical snapshot of metric values.
/// </summary>
/// <param name="Timestamp">The snapshot timestamp.</param>
/// <param name="Values">The metric values captured at the timestamp.</param>
public sealed record MetricHistoryPoint(DateTimeOffset Timestamp, IReadOnlyDictionary<string, double> Values);
