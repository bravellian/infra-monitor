using Bravellian.InfraMonitor.Metrics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

namespace Bravellian.InfraMonitor.Metrics.AspNetCore;

/// <summary>
/// Service registration helpers for Bravellian metrics in ASP.NET Core.
/// </summary>
public static class BravellianMetricsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the meter provider and OpenTelemetry metrics pipeline.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for metrics options.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBravellianMetrics(
        this IServiceCollection services,
        Action<BravellianMetricsOptions>? configure = null)
    {
        var options = new BravellianMetricsOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<BravellianMeterProvider>(sp =>
            new BravellianMeterProvider(
                sp.GetRequiredService<System.Diagnostics.Metrics.IMeterFactory>(),
                options.Meter));

        services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                builder.AddMeter(options.Meter.MeterName);

                if (options.EnableAspNetCoreInstrumentation)
                {
                    builder.AddAspNetCoreInstrumentation();
                }

                if (options.EnableRuntimeInstrumentation)
                {
                    builder.AddRuntimeInstrumentation();
                }

                if (options.EnableProcessInstrumentation)
                {
                    builder.AddProcessInstrumentation();
                }

                if (options.EnablePrometheusExporter)
                {
                    builder.AddPrometheusExporter(exporterOptions =>
                    {
                        exporterOptions.ScrapeEndpointPath = options.PrometheusEndpointPath;
                        exporterOptions.ScrapeResponseCacheDurationMilliseconds =
                            options.PrometheusScrapeResponseCacheMilliseconds;
                    });
                }
            });

        return services;
    }
}
