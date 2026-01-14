using System.Collections.Generic;

namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

public sealed record PrometheusMetricSample(
    string Name,
    IReadOnlyDictionary<string, string> Labels,
    string Value,
    long? Timestamp);
