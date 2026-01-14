using System;
using System.Collections.Generic;

namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

public sealed class PrometheusScrapeSnapshot
{
    public PrometheusScrapeSnapshot(
        IReadOnlyList<PrometheusMetricSample> samples,
        IReadOnlyDictionary<string, PrometheusMetricMetadata> metadata,
        string rawPayload,
        DateTimeOffset retrievedAt)
    {
        Samples = samples;
        Metadata = metadata;
        RawPayload = rawPayload;
        RetrievedAt = retrievedAt;
    }

    public IReadOnlyList<PrometheusMetricSample> Samples { get; }
    public IReadOnlyDictionary<string, PrometheusMetricMetadata> Metadata { get; }
    public string RawPayload { get; }
    public DateTimeOffset RetrievedAt { get; }
}
