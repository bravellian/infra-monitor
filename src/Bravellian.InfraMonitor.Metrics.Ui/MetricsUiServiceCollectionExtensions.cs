using System;
using Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Bravellian.InfraMonitor.Metrics.Ui;

/// <summary>
/// Service registration helpers for the metrics UI.
/// </summary>
public static class MetricsUiServiceCollectionExtensions
{
    /// <summary>
    /// Adds Razor Pages and services required for the metrics UI.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The MVC builder.</returns>
    public static IMvcBuilder AddBravellianMetricsUi(this IServiceCollection services)
    {
        var builder = services.AddRazorPages()
            .AddApplicationPart(typeof(MetricsUiServiceCollectionExtensions).Assembly);

        services.AddHttpClient<MetricsScrapeService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<MetricsHistoryStore>();
        services.AddSingleton<InfraMonitorAppMetrics>();

        return builder;
    }
}
