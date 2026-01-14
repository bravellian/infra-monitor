namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

public sealed record MetricHistoryPoint(DateTimeOffset Timestamp, IReadOnlyDictionary<string, double> Values);
