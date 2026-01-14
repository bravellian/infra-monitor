using System;
using System.Collections.Generic;

namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

public sealed class MetricsHistoryStore
{
    private readonly Dictionary<string, List<MetricHistoryPoint>> history =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock @lock = new();
    private readonly int maxPoints;

    public MetricsHistoryStore(int maxPoints = 120)
    {
        this.maxPoints = maxPoints;
    }

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
