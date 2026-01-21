using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

/// <summary>
/// Parses Prometheus text format into samples and metadata.
/// </summary>
public static class PrometheusMetricsParser
{
    /// <summary>
    /// Parses a Prometheus payload into a structured snapshot.
    /// </summary>
    /// <param name="payload">The raw Prometheus scrape text.</param>
    /// <returns>The parsed snapshot.</returns>
    public static PrometheusScrapeSnapshot Parse(string payload)
    {
        var samples = new List<PrometheusMetricSample>();
        var metadata = new Dictionary<string, PrometheusMetricMetadata>(StringComparer.Ordinal);

        foreach (var rawLine in payload.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("# HELP ", StringComparison.Ordinal))
            {
                ParseHelp(line, metadata);
                continue;
            }

            if (line.StartsWith("# TYPE ", StringComparison.Ordinal))
            {
                ParseType(line, metadata);
                continue;
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            if (TryParseSample(line, out var sample))
            {
                samples.Add(sample);
            }
        }

        return new PrometheusScrapeSnapshot(samples, metadata, payload, DateTimeOffset.UtcNow);
    }

    private static void ParseHelp(string line, IDictionary<string, PrometheusMetricMetadata> metadata)
    {
        var parts = line.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return;
        }

        var name = parts[2];
        var help = parts.Length == 4 ? parts[3] : null;
        if (!metadata.TryGetValue(name, out var existing))
        {
            metadata[name] = new PrometheusMetricMetadata(existing?.Type, help);
            return;
        }

        metadata[name] = existing with { Help = help };
    }

    private static void ParseType(string line, IDictionary<string, PrometheusMetricMetadata> metadata)
    {
        var parts = line.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return;
        }

        var name = parts[2];
        var type = parts[3];
        if (!metadata.TryGetValue(name, out var existing))
        {
            metadata[name] = new PrometheusMetricMetadata(type, existing?.Help);
            return;
        }

        metadata[name] = existing with { Type = type };
    }

    private static bool TryParseSample(string line, out PrometheusMetricSample sample)
    {
        sample = default!;

        var spaceIndex = line.IndexOf(' ');
        if (spaceIndex <= 0)
        {
            return false;
        }

        var left = line[..spaceIndex];
        var right = line[(spaceIndex + 1)..].Trim();
        if (right.Length == 0)
        {
            return false;
        }

        var valuePart = right;
        long? timestamp = null;
        var valueParts = right.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (valueParts.Length >= 1)
        {
            valuePart = valueParts[0];
        }

        if (valueParts.Length >= 2 && long.TryParse(valueParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            timestamp = parsed;
        }

        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        var name = left;
        var labelStart = left.IndexOf('{');
        if (labelStart >= 0)
        {
            name = left[..labelStart];
            var labelEnd = left.LastIndexOf('}');
            if (labelEnd > labelStart)
            {
                var labelBody = left[(labelStart + 1)..labelEnd];
                ParseLabels(labelBody, labels);
            }
        }

        sample = new PrometheusMetricSample(name, labels, valuePart, timestamp);
        return true;
    }

    private static void ParseLabels(string labelBody, IDictionary<string, string> labels)
    {
        var span = labelBody.AsSpan();
        var index = 0;

        while (index < span.Length)
        {
            var equalsIndex = span[index..].IndexOf('=');
            if (equalsIndex < 0)
            {
                break;
            }

            var keySpan = span.Slice(index, equalsIndex).Trim();
            index += equalsIndex + 1;
            if (index >= span.Length || span[index] != '"')
            {
                break;
            }

            index++;
            var valueBuilder = new StringBuilder();
            while (index < span.Length)
            {
                var ch = span[index++];
                if (ch == '\\' && index < span.Length)
                {
                    var escaped = span[index++];
                    valueBuilder.Append(escaped switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        '"' => '"',
                        '\\' => '\\',
                        _ => escaped
                    });
                    continue;
                }

                if (ch == '"')
                {
                    break;
                }

                valueBuilder.Append(ch);
            }

            labels[keySpan.ToString()] = valueBuilder.ToString();

            var commaIndex = span[index..].IndexOf(',');
            if (commaIndex < 0)
            {
                break;
            }

            index += commaIndex + 1;
        }
    }
}
