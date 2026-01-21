using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;
using Bravellian.InfraMonitor.Metrics.Ui.Services.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;

namespace Bravellian.InfraMonitor.Metrics.Ui.Pages.Metrics;

/// <summary>
/// Metrics dashboard page model that aggregates scrape data for display.
/// </summary>
public class IndexModel : PageModel
{
    private const int SampleLimit = 200;
    private const int DefaultRefreshSeconds = 20;
    private readonly IMetricsSetupStore setupStore;
    private readonly MetricsScrapeService scrapeService;
    private readonly MetricsHistoryStore historyStore;
    private readonly InfraMonitorAppMetrics appMetrics;
    private readonly ILogger<IndexModel> logger;

    /// <summary>
    /// Initializes the page model with required services.
    /// </summary>
    /// <param name="setupStore">The setup store for user configuration.</param>
    /// <param name="scrapeService">The metrics scraping service.</param>
    /// <param name="historyStore">The metrics history store.</param>
    /// <param name="appMetrics">The application metrics recorder.</param>
    /// <param name="logger">The logger instance.</param>
    public IndexModel(
        IMetricsSetupStore setupStore,
        MetricsScrapeService scrapeService,
        MetricsHistoryStore historyStore,
        InfraMonitorAppMetrics appMetrics,
        ILogger<IndexModel> logger)
    {
        this.setupStore = setupStore;
        this.scrapeService = scrapeService;
        this.historyStore = historyStore;
        this.appMetrics = appMetrics;
        this.logger = logger;
    }

    /// <summary>
    /// Gets the auto-refresh interval in seconds.
    /// </summary>
    public int AutoRefreshSeconds { get; private set; } = DefaultRefreshSeconds;

    /// <summary>
    /// Gets a value indicating whether any endpoints are configured.
    /// </summary>
    public bool HasEndpoints { get; private set; }

    /// <summary>
    /// Gets the number of configured endpoints.
    /// </summary>
    public int EndpointCount { get; private set; }

    /// <summary>
    /// Gets the list of pinned metric names.
    /// </summary>
    public IReadOnlyList<string> PinnedMetrics { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// Gets the services to render on the dashboard.
    /// </summary>
    public IReadOnlyList<MetricsServiceViewModel> Services { get; private set; } = Array.Empty<MetricsServiceViewModel>();

    /// <summary>
    /// Handles the initial page load.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles a partial refresh request for the metrics content.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The partial view result.</returns>
    public async Task<IActionResult> OnGetRefreshAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return new PartialViewResult
        {
            ViewName = "Metrics/_MetricsContent",
            ViewData = new ViewDataDictionary<IndexModel>(ViewData, this)
        };
    }

    /// <summary>
    /// Records a demo email metric and redirects back to the page.
    /// </summary>
    /// <returns>The redirect result.</returns>
    public IActionResult OnPostDemoEmail()
    {
        appMetrics.RecordDemoEmail();
        return RedirectToPage();
    }

    /// <summary>
    /// Pins a metric name for dashboard display.
    /// </summary>
    /// <param name="metricName">The metric name to pin.</param>
    /// <returns>The redirect result.</returns>
    public IActionResult OnPostPinMetric(string? metricName)
    {
        if (string.IsNullOrWhiteSpace(metricName))
        {
            return RedirectToPage();
        }

        if (!setupStore.TryGetPinnedMetrics(HttpContext, out var pinned))
        {
            pinned = Array.Empty<string>();
        }

        var updated = pinned.ToList();
        var trimmed = metricName.Trim();
        if (!updated.Any(item => item.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            updated.Add(trimmed);
            setupStore.SetPinnedMetrics(HttpContext, updated);
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Removes a pinned metric name.
    /// </summary>
    /// <param name="metricName">The metric name to unpin.</param>
    /// <returns>The redirect result.</returns>
    public IActionResult OnPostUnpinMetric(string? metricName)
    {
        if (string.IsNullOrWhiteSpace(metricName))
        {
            return RedirectToPage();
        }

        if (!setupStore.TryGetPinnedMetrics(HttpContext, out var pinned))
        {
            pinned = Array.Empty<string>();
        }

        var updated = pinned
            .Where(item => !item.Equals(metricName.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
        setupStore.SetPinnedMetrics(HttpContext, updated);
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!setupStore.TryGetMetricsEndpoints(HttpContext, out var endpoints))
        {
            endpoints = Array.Empty<MetricsEndpointRegistration>();
        }

        AutoRefreshSeconds = DefaultRefreshSeconds;
        if (setupStore.TryGetMetricsRefreshSeconds(HttpContext, out var refreshSeconds))
        {
            AutoRefreshSeconds = refreshSeconds;
        }

        if (!setupStore.TryGetPinnedMetrics(HttpContext, out var pinned))
        {
            pinned = Array.Empty<string>();
        }

        PinnedMetrics = pinned;
        EndpointCount = endpoints.Count;
        HasEndpoints = EndpointCount > 0;
        if (!HasEndpoints)
        {
            return;
        }

        var tasks = endpoints.Select(endpoint => scrapeService.ScrapeAsync(endpoint, cancellationToken)).ToArray();
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        Services = results
            .GroupBy(report => report.Registration.ServiceName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => BuildService(group.Key, group.ToList(), PinnedMetrics))
            .ToList();
    }

    private MetricsServiceViewModel BuildService(
        string serviceName,
        IReadOnlyList<MetricsEndpointReport> reports,
        IReadOnlyList<string> pinnedMetrics)
    {
        var instances = reports
            .Select(BuildInstance)
            .OrderBy(instance => instance.Registration.InstanceName, StringComparer.Ordinal)
            .ToList();

        var aggregate = BuildAggregate(serviceName, reports, pinnedMetrics);
        return new MetricsServiceViewModel(serviceName, aggregate, instances);
    }

    private MetricsAggregateViewModel BuildAggregate(
        string serviceName,
        IReadOnlyList<MetricsEndpointReport> reports,
        IReadOnlyList<string> pinnedMetrics)
    {
        var successfulReports = reports.Where(report => report.Snapshot is not null).ToList();
        var snapshots = successfulReports.Select(report => report.Snapshot!).ToList();
        var pinnedDefinitions = BuildPinnedDefinitions(pinnedMetrics, snapshots);

        var aggregateValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var snapshot in snapshots)
        {
            var fixedValues = GetMetricValues(snapshot, dashboardDefinitions);
            AddValues(aggregateValues, fixedValues);

            var pinnedValues = GetMetricValues(snapshot, pinnedDefinitions);
            AddValues(aggregateValues, pinnedValues);
        }

        var historyKey = GetServiceHistoryKey(serviceName);
        if (aggregateValues.Count > 0)
        {
            var latestTimestamp = snapshots.Count > 0
                ? snapshots.Max(item => item.RetrievedAt)
                : DateTimeOffset.UtcNow;
            historyStore.AddSnapshot(historyKey, latestTimestamp, aggregateValues);
        }

        var history = historyStore.GetHistory(historyKey);
        var fixedCards = BuildDashboardCards(aggregateValues, history, dashboardDefinitions);
        var fixedCharts = BuildDashboardCharts(history, historyKey, dashboardDefinitions);
        var pinnedCards = BuildDashboardCards(aggregateValues, history, pinnedDefinitions);
        var pinnedCharts = BuildDashboardCharts(history, historyKey, pinnedDefinitions);
        var seriesSummaries = BuildAggregateSeriesSummaries(snapshots);
        var samples = BuildAggregateSamples(snapshots);
        var totalSamples = snapshots.Sum(snapshot => snapshot.Samples.Count);
        var failedCount = reports.Count(report => report.Snapshot is null);
        var averageDurationMs = reports.Count > 0
            ? reports.Average(report => report.Duration.TotalMilliseconds)
            : 0;
        var latestScrape = snapshots.Count > 0
            ? snapshots.Max(item => item.RetrievedAt)
            : (DateTimeOffset?)null;

        return new MetricsAggregateViewModel(
            fixedCards,
            fixedCharts,
            pinnedCards,
            pinnedCharts,
            seriesSummaries,
            samples,
            totalSamples,
            reports.Count,
            successfulReports.Count,
            failedCount,
            averageDurationMs,
            latestScrape);
    }

    private MetricsInstanceViewModel BuildInstance(MetricsEndpointReport report)
    {
        if (report.Snapshot is null)
        {
            logger.LogWarning("Metrics scrape failed for {Endpoint}. {Error}", report.Registration.EndpointUrl, report.Error);
            return new MetricsInstanceViewModel(
                report.Registration,
                null,
                report.Error,
                report.Duration,
                Array.Empty<DashboardMetricCardViewModel>(),
                Array.Empty<DashboardChartViewModel>(),
                Array.Empty<MetricSeriesSummaryViewModel>(),
                Array.Empty<MetricSampleViewModel>(),
                0);
        }

        var snapshot = report.Snapshot;
        var dashboardValues = GetMetricValues(snapshot, dashboardDefinitions);
        if (dashboardValues.Count > 0)
        {
            historyStore.AddSnapshot(GetInstanceHistoryKey(report.Registration), snapshot.RetrievedAt, dashboardValues);
        }

        var history = historyStore.GetHistory(GetInstanceHistoryKey(report.Registration));
        var dashboardCards = BuildDashboardCards(dashboardValues, history, dashboardDefinitions);
        var dashboardCharts = BuildDashboardCharts(history, GetInstanceHistoryKey(report.Registration), dashboardDefinitions);
        var series = snapshot.Samples
            .GroupBy(sample => sample.Name, StringComparer.Ordinal)
            .Select(group =>
            {
                snapshot.Metadata.TryGetValue(group.Key, out var metadata);
                return new MetricSeriesSummaryViewModel(
                    group.Key,
                    metadata?.Type,
                    metadata?.Help,
                    group.Count());
            })
            .OrderByDescending(summary => summary.SeriesCount)
            .ThenBy(summary => summary.Name, StringComparer.Ordinal)
            .ToList();

        var samples = snapshot.Samples
            .Take(SampleLimit)
            .Select(sample =>
            {
                snapshot.Metadata.TryGetValue(sample.Name, out var metadata);
                return new MetricSampleViewModel(
                    sample.Name,
                    FormatLabels(sample.Labels),
                    sample.Value,
                    metadata?.Type,
                    metadata?.Help);
            })
            .ToList();

        return new MetricsInstanceViewModel(
            report.Registration,
            snapshot,
            null,
            report.Duration,
            dashboardCards,
            dashboardCharts,
            series,
            samples,
            snapshot.Samples.Count);
    }

    private static string FormatLabels(IReadOnlyDictionary<string, string> labels)
    {
        if (labels.Count == 0)
        {
            return "none";
        }

        return string.Join(", ", labels.Select(pair => $"{pair.Key}=\"{pair.Value}\""));
    }

    private static IReadOnlyDictionary<string, double> GetMetricValues(
        PrometheusScrapeSnapshot snapshot,
        IReadOnlyList<MetricDashboardDefinition> definitions)
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in definitions)
        {
            if (TryGetMetricValue(snapshot, definition, out var value))
            {
                values[definition.Key] = value;
            }
        }

        return values;
    }

    private static IReadOnlyList<DashboardMetricCardViewModel> BuildDashboardCards(
        IReadOnlyDictionary<string, double> latestValues,
        IReadOnlyList<MetricHistoryPoint> history,
        IReadOnlyList<MetricDashboardDefinition> definitions)
    {
        var cards = new List<DashboardMetricCardViewModel>();
        foreach (var definition in definitions)
        {
            if (!latestValues.TryGetValue(definition.Key, out var latest))
            {
                continue;
            }

            var valueText = definition.Kind == MetricValueKind.Counter
                ? FormatRate(history, definition.Key, definitions)
                : FormatValue(latest, definition.Unit);

            cards.Add(new DashboardMetricCardViewModel(definition.Title, valueText, definition.Unit, definition.Description));
        }

        return cards;
    }

    private static IReadOnlyList<DashboardChartViewModel> BuildDashboardCharts(
        IReadOnlyList<MetricHistoryPoint> history,
        string chartScope,
        IReadOnlyList<MetricDashboardDefinition> definitions)
    {
        if (history.Count < 2)
        {
            return Array.Empty<DashboardChartViewModel>();
        }

        var charts = new List<DashboardChartViewModel>();
        foreach (var definition in definitions)
        {
            if (definition.Kind == MetricValueKind.Counter)
            {
                var ratePoints = BuildRateSeries(history, definition.Key);
                if (ratePoints.Count > 1)
                {
                    charts.Add(new DashboardChartViewModel(
                        BuildChartId(definition.Key, chartScope),
                        definition.Title,
                        $"{definition.Unit}/s",
                        true,
                        ratePoints));
                }

                continue;
            }

            var points = BuildGaugeSeries(history, definition.Key);
            if (points.Count > 1)
            {
                charts.Add(new DashboardChartViewModel(
                    BuildChartId(definition.Key, chartScope),
                    definition.Title,
                    definition.Unit,
                    false,
                    points));
            }
        }

        return charts;
    }

    private static IReadOnlyList<MetricChartPointViewModel> BuildRateSeries(
        IReadOnlyList<MetricHistoryPoint> history,
        string key)
    {
        var points = new List<MetricChartPointViewModel>();
        for (var i = 1; i < history.Count; i++)
        {
            if (!history[i - 1].Values.TryGetValue(key, out var previous) ||
                !history[i].Values.TryGetValue(key, out var current))
            {
                continue;
            }

            var delta = current - previous;
            if (delta < 0)
            {
                continue;
            }

            var seconds = (history[i].Timestamp - history[i - 1].Timestamp).TotalSeconds;
            if (seconds <= 0)
            {
                continue;
            }

            var rate = delta / seconds;
            points.Add(new MetricChartPointViewModel(history[i].Timestamp, rate));
        }

        return points;
    }

    private static IReadOnlyList<MetricChartPointViewModel> BuildGaugeSeries(
        IReadOnlyList<MetricHistoryPoint> history,
        string key)
    {
        var points = new List<MetricChartPointViewModel>();
        foreach (var item in history)
        {
            if (!item.Values.TryGetValue(key, out var value))
            {
                continue;
            }

            points.Add(new MetricChartPointViewModel(item.Timestamp, value));
        }

        return points;
    }

    private static string FormatRate(
        IReadOnlyList<MetricHistoryPoint> history,
        string key,
        IReadOnlyList<MetricDashboardDefinition> definitions)
    {
        var series = BuildRateSeries(history, key);
        if (series.Count == 0)
        {
            return "n/a";
        }

        var latest = series[^1].Value;
        return $"{latest:F2} {GetRateUnit(key, definitions)}";
    }

    private static string GetRateUnit(string key, IReadOnlyList<MetricDashboardDefinition> definitions)
    {
        var definition = definitions.FirstOrDefault(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (definition is null || string.IsNullOrWhiteSpace(definition.Unit))
        {
            return "per sec";
        }

        return $"{definition.Unit}/s";
    }

    private static string FormatValue(double value, string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            return value.ToString("N2", CultureInfo.InvariantCulture);
        }

        return $"{value.ToString("N2", CultureInfo.InvariantCulture)} {unit}";
    }

    private static string BuildChartId(string key, string scope)
        => $"metric-chart-{SanitizeId(scope)}-{SanitizeId(key)}";

    private static string SanitizeId(string value)
    {
        var buffer = new char[value.Length];
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            buffer[i] = char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-';
        }

        return new string(buffer);
    }

    private static string GetInstanceHistoryKey(MetricsEndpointRegistration registration)
        => $"instance:{registration.ServiceName}:{registration.InstanceName}:{registration.EndpointUrl}";

    private static string GetServiceHistoryKey(string serviceName)
        => $"service:{serviceName}";

    private static void AddValues(IDictionary<string, double> target, IReadOnlyDictionary<string, double> values)
    {
        foreach (var pair in values)
        {
            target[pair.Key] = target.TryGetValue(pair.Key, out var current)
                ? current + pair.Value
                : pair.Value;
        }
    }

    private static IReadOnlyList<MetricSeriesSummaryViewModel> BuildAggregateSeriesSummaries(
        IReadOnlyList<PrometheusScrapeSnapshot> snapshots)
    {
        var metadata = snapshots
            .SelectMany(snapshot => snapshot.Metadata)
            .GroupBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.Ordinal);

        return snapshots
            .SelectMany(snapshot => snapshot.Samples)
            .GroupBy(sample => sample.Name, StringComparer.Ordinal)
            .Select(group => new MetricSeriesSummaryViewModel(
                group.Key,
                metadata.TryGetValue(group.Key, out var meta) ? meta.Type : null,
                metadata.TryGetValue(group.Key, out var metaHelp) ? metaHelp.Help : null,
                group.Count()))
            .OrderByDescending(summary => summary.SeriesCount)
            .ThenBy(summary => summary.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<MetricSampleViewModel> BuildAggregateSamples(
        IReadOnlyList<PrometheusScrapeSnapshot> snapshots)
    {
        var metadata = snapshots
            .SelectMany(snapshot => snapshot.Metadata)
            .GroupBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.Ordinal);

        var aggregated = snapshots
            .SelectMany(snapshot => snapshot.Samples)
            .Select(sample => new
            {
                sample.Name,
                Labels = FormatLabels(sample.Labels),
                Value = double.TryParse(sample.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0
            })
            .GroupBy(sample => $"{sample.Name}|{sample.Labels}", StringComparer.Ordinal)
            .Select(group =>
            {
                var first = group.First();
                var sum = group.Sum(item => item.Value);
                metadata.TryGetValue(first.Name, out var meta);
                return new
                {
                    first.Name,
                    first.Labels,
                    Sum = sum,
                    meta?.Type,
                    meta?.Help
                };
            })
            .OrderByDescending(item => item.Sum)
            .Take(SampleLimit)
            .Select(item => new MetricSampleViewModel(
                item.Name,
                item.Labels,
                item.Sum.ToString("N2", CultureInfo.InvariantCulture),
                item.Type,
                item.Help))
            .ToList();

        return aggregated;
    }

    private static IReadOnlyList<MetricDashboardDefinition> BuildPinnedDefinitions(
        IReadOnlyList<string> pinnedMetrics,
        IReadOnlyList<PrometheusScrapeSnapshot> snapshots)
    {
        if (pinnedMetrics.Count == 0)
        {
            return Array.Empty<MetricDashboardDefinition>();
        }

        var definitions = new List<MetricDashboardDefinition>();
        foreach (var metric in pinnedMetrics)
        {
            var meta = snapshots
                .Select(snapshot =>
                    snapshot.Metadata.TryGetValue(metric, out var metadata) ? metadata : null)
                .FirstOrDefault(value => value is not null);

            var kind = ResolveMetricKind(metric, meta?.Type);
            definitions.Add(new MetricDashboardDefinition(
                metric,
                metric,
                meta?.Help,
                null,
                kind,
                metric));
        }

        return definitions;
    }

    private static MetricValueKind ResolveMetricKind(string metricName, string? type)
    {
        if (string.Equals(type, "counter", StringComparison.OrdinalIgnoreCase))
        {
            return MetricValueKind.Counter;
        }

        if (metricName.EndsWith("_total", StringComparison.OrdinalIgnoreCase) ||
            metricName.EndsWith("_count", StringComparison.OrdinalIgnoreCase))
        {
            return MetricValueKind.Counter;
        }

        return MetricValueKind.Gauge;
    }

    private static bool TryGetMetricValue(
        PrometheusScrapeSnapshot snapshot,
        MetricDashboardDefinition definition,
        out double value)
    {
        value = 0;
        var found = false;
        foreach (var sample in snapshot.Samples)
        {
            if (!definition.MetricNames.Any(name => name.Equals(sample.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (double.TryParse(sample.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                value += parsed;
                found = true;
            }
        }

        return found;
    }

    private static readonly IReadOnlyList<MetricDashboardDefinition> dashboardDefinitions =
    [
        new MetricDashboardDefinition(
            "process.cpu.count",
            "CPU Count",
            "Logical CPU count reported by the runtime.",
            "cores",
            MetricValueKind.Gauge,
            "process.cpu.count",
            "process_cpu_count"),
        new MetricDashboardDefinition(
            "process.cpu.time",
            "CPU Time",
            "Total CPU time consumed by the process.",
            "s",
            MetricValueKind.Counter,
            "process.cpu.time",
            "process_cpu_time_seconds_total",
            "process_cpu_time"),
        new MetricDashboardDefinition(
            "process.memory.usage",
            "Working Set",
            "Current working set memory size.",
            "bytes",
            MetricValueKind.Gauge,
            "process.memory.usage",
            "process_memory_usage_bytes",
            "process_working_set_bytes"),
        new MetricDashboardDefinition(
            "process.thread.count",
            "Thread Count",
            "Total OS threads in the process.",
            "threads",
            MetricValueKind.Gauge,
            "process.thread.count",
            "process_thread_count",
            "process_runtime_dotnet_thread_count"),
        new MetricDashboardDefinition(
            "process.runtime.dotnet.gc.heap.size",
            "GC Heap Size",
            "Managed heap size across generations.",
            "bytes",
            MetricValueKind.Gauge,
            "process.runtime.dotnet.gc.heap.size",
            "process_runtime_dotnet_gc_heap_size_bytes",
            "dotnet_gc_heap_size_bytes"),
        new MetricDashboardDefinition(
            "process.runtime.dotnet.gc.total_allocated",
            "GC Allocated",
            "Total bytes allocated by the GC.",
            "bytes",
            MetricValueKind.Counter,
            "process.runtime.dotnet.gc.total_allocated",
            "process_runtime_dotnet_gc_total_allocated_bytes",
            "dotnet_gc_allocated_bytes_total"),
        new MetricDashboardDefinition(
            "process.runtime.dotnet.thread_pool.queue.length",
            "ThreadPool Queue",
            "Work items waiting in the thread pool.",
            "items",
            MetricValueKind.Gauge,
            "process.runtime.dotnet.thread_pool.queue.length",
            "process_runtime_dotnet_thread_pool_queue_length"),
        new MetricDashboardDefinition(
            "http.server.request.duration",
            "HTTP Requests",
            "Observed HTTP request volume.",
            "requests",
            MetricValueKind.Counter,
            "http.server.request.duration_count",
            "http_server_request_duration_seconds_count",
            "http_server_requests_total"),
        new MetricDashboardDefinition(
            "bravellian.infra_monitor.demo_emails_sent",
            "Demo Emails Sent",
            "Custom demo counter for email sends.",
            "emails",
            MetricValueKind.Counter,
            "bravellian.infra_monitor.demo_emails_sent",
            "bravellian_infra_monitor_demo_emails_sent",
            "bravellian_infra_monitor_demo_emails_sent_total")
    ];
}

/// <summary>
/// Represents a service and its aggregated metrics data.
/// </summary>
/// <param name="ServiceName">The logical service name.</param>
/// <param name="Aggregate">The aggregate view model.</param>
/// <param name="Instances">The instance view models.</param>
public sealed record MetricsServiceViewModel(
    string ServiceName,
    MetricsAggregateViewModel Aggregate,
    IReadOnlyList<MetricsInstanceViewModel> Instances);

/// <summary>
/// Represents aggregate metrics across all instances for a service.
/// </summary>
/// <param name="DashboardCards">The dashboard cards for fixed metrics.</param>
/// <param name="DashboardCharts">The dashboard charts for fixed metrics.</param>
/// <param name="PinnedCards">The dashboard cards for pinned metrics.</param>
/// <param name="PinnedCharts">The dashboard charts for pinned metrics.</param>
/// <param name="SeriesSummaries">The metric series summaries.</param>
/// <param name="Samples">The aggregated sample list.</param>
/// <param name="TotalSamples">The total sample count.</param>
/// <param name="InstanceCount">The number of instances in the aggregate.</param>
/// <param name="SuccessfulInstances">The count of successful scrapes.</param>
/// <param name="FailedInstances">The count of failed scrapes.</param>
/// <param name="AverageScrapeDurationMs">The average scrape duration in milliseconds.</param>
/// <param name="LatestScrape">The latest scrape timestamp.</param>
public sealed record MetricsAggregateViewModel(
    IReadOnlyList<DashboardMetricCardViewModel> DashboardCards,
    IReadOnlyList<DashboardChartViewModel> DashboardCharts,
    IReadOnlyList<DashboardMetricCardViewModel> PinnedCards,
    IReadOnlyList<DashboardChartViewModel> PinnedCharts,
    IReadOnlyList<MetricSeriesSummaryViewModel> SeriesSummaries,
    IReadOnlyList<MetricSampleViewModel> Samples,
    int TotalSamples,
    int InstanceCount,
    int SuccessfulInstances,
    int FailedInstances,
    double AverageScrapeDurationMs,
    DateTimeOffset? LatestScrape);

/// <summary>
/// Represents metrics data for a single service instance.
/// </summary>
/// <param name="Registration">The endpoint registration.</param>
/// <param name="Snapshot">The scrape snapshot if available.</param>
/// <param name="Error">The scrape error message.</param>
/// <param name="Duration">The scrape duration.</param>
/// <param name="DashboardCards">The dashboard cards.</param>
/// <param name="DashboardCharts">The dashboard charts.</param>
/// <param name="SeriesSummaries">The metric series summaries.</param>
/// <param name="Samples">The sample list.</param>
/// <param name="TotalSamples">The total sample count.</param>
public sealed record MetricsInstanceViewModel(
    MetricsEndpointRegistration Registration,
    PrometheusScrapeSnapshot? Snapshot,
    string? Error,
    TimeSpan Duration,
    IReadOnlyList<DashboardMetricCardViewModel> DashboardCards,
    IReadOnlyList<DashboardChartViewModel> DashboardCharts,
    IReadOnlyList<MetricSeriesSummaryViewModel> SeriesSummaries,
    IReadOnlyList<MetricSampleViewModel> Samples,
    int TotalSamples);

/// <summary>
/// Summarizes the number of series for a metric name.
/// </summary>
/// <param name="Name">The metric name.</param>
/// <param name="Type">The metric type.</param>
/// <param name="Help">The help text.</param>
/// <param name="SeriesCount">The number of series.</param>
public sealed record MetricSeriesSummaryViewModel(
    string Name,
    string? Type,
    string? Help,
    int SeriesCount);

/// <summary>
/// Represents a display sample for a metric.
/// </summary>
/// <param name="Name">The metric name.</param>
/// <param name="Labels">The formatted labels.</param>
/// <param name="Value">The formatted value.</param>
/// <param name="Type">The metric type.</param>
/// <param name="Help">The help text.</param>
public sealed record MetricSampleViewModel(
    string Name,
    string Labels,
    string Value,
    string? Type,
    string? Help);

/// <summary>
/// Represents a metric card for the dashboard.
/// </summary>
/// <param name="Title">The card title.</param>
/// <param name="Value">The metric value.</param>
/// <param name="Unit">The metric unit.</param>
/// <param name="Description">The metric description.</param>
public sealed record DashboardMetricCardViewModel(
    string Title,
    string Value,
    string? Unit,
    string? Description);

/// <summary>
/// Represents a chart for a metric time series.
/// </summary>
/// <param name="Id">The chart element id.</param>
/// <param name="Title">The chart title.</param>
/// <param name="Unit">The unit of measure.</param>
/// <param name="IsRate">Whether the series is a rate series.</param>
/// <param name="Points">The chart data points.</param>
public sealed record DashboardChartViewModel(
    string Id,
    string Title,
    string? Unit,
    bool IsRate,
    IReadOnlyList<MetricChartPointViewModel> Points);

/// <summary>
/// Represents a point in a metric chart series.
/// </summary>
/// <param name="Timestamp">The point timestamp.</param>
/// <param name="Value">The point value.</param>
public sealed record MetricChartPointViewModel(DateTimeOffset Timestamp, double Value);

/// <summary>
/// Defines a dashboard metric and its display configuration.
/// </summary>
/// <param name="Key">The internal key used for tracking history.</param>
/// <param name="Title">The display title.</param>
/// <param name="Description">The description for the metric.</param>
/// <param name="Unit">The unit of measure.</param>
/// <param name="Kind">The metric value kind.</param>
/// <param name="MetricNames">The underlying metric names to match.</param>
public sealed record MetricDashboardDefinition(
    string Key,
    string Title,
    string? Description,
    string? Unit,
    MetricValueKind Kind,
    params string[] MetricNames);

/// <summary>
/// Identifies whether a metric should be treated as a gauge or a counter.
/// </summary>
public enum MetricValueKind
{
    Gauge,
    Counter
}
