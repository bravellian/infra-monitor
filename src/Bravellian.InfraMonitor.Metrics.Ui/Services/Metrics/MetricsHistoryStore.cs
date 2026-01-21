using System;
using System.Collections.Generic;

namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

/// <summary>
/// Maintains a bounded history of metric snapshots per key.
/// </summary>
public sealed class MetricsHistoryStore
{
    private readonly Dictionary<string, List<MetricHistoryPoint>> history =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock @lock = new();
    private readonly int maxPoints;

    /// <summary>
    /// Initializes the history store with the desired capacity.
    /// </summary>
    /// <param name="maxPoints">The maximum number of points to retain per key.</param>
    public MetricsHistoryStore(int maxPoints = 120)
    {
        this.maxPoints = maxPoints;
    }

    /// <summary>
    /// Adds a new snapshot for the specified key.
    /// </summary>
    /// <param name="key">The key that identifies the metric series.</param>
    /// <param name="timestamp">The time of the snapshot.</param>
    /// <param name="values">The metric values at the time of the snapshot.</param>
    public void AddSnapshot(string key, DateTimeOffset timestamp, IReadOnlyDictionary<string, double> values)
    {
        lock (@lock)
        {
            if (!history.TryGetValue(key, out var points))
            {
                points = new List<MetricHistoryPoint>();
                history[key] = points;
            }

            points.Add(new MetricHistoryPoint(timestamp, values));
            if (points.Count > maxPoints)
            {
                points.RemoveRange(0, points.Count - maxPoints);
            }
        }
    }

    /// <summary>
    /// Retrieves the history for the specified key.
    /// </summary>
    /// <param name="key">The key that identifies the metric series.</param>
    /// <returns>The recorded history points, or an empty list.</returns>
    public IReadOnlyList<MetricHistoryPoint> GetHistory(string key)
    {
        lock (@lock)
        {
            if (!history.TryGetValue(key, out var points))
            {
                return Array.Empty<MetricHistoryPoint>();
            }

            return points.ToArray();
        }
    }
}
