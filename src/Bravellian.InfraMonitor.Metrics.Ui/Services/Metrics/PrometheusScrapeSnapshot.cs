using System;
using System.Collections.Generic;

namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

/// <summary>
/// Represents a parsed snapshot of a Prometheus scrape payload.
/// </summary>
public sealed class PrometheusScrapeSnapshot
{
    /// <summary>
    /// Initializes a new snapshot.
    /// </summary>
    /// <param name="samples">The parsed metric samples.</param>
    /// <param name="metadata">The metric metadata keyed by metric name.</param>
    /// <param name="rawPayload">The raw scrape payload.</param>
    /// <param name="retrievedAt">The time the payload was retrieved.</param>
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

    /// <summary>
    /// Gets the parsed metric samples.
    /// </summary>
    public IReadOnlyList<PrometheusMetricSample> Samples { get; }

    /// <summary>
    /// Gets the parsed metric metadata keyed by metric name.
    /// </summary>
    public IReadOnlyDictionary<string, PrometheusMetricMetadata> Metadata { get; }

    /// <summary>
    /// Gets the raw scrape payload.
    /// </summary>
    public string RawPayload { get; }

    /// <summary>
    /// Gets the timestamp when the payload was retrieved.
    /// </summary>
    public DateTimeOffset RetrievedAt { get; }
}
