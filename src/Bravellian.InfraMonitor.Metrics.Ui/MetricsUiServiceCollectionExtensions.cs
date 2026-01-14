using System;
using Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Bravellian.InfraMonitor.Metrics.Ui;

public static class MetricsUiServiceCollectionExtensions
{
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
