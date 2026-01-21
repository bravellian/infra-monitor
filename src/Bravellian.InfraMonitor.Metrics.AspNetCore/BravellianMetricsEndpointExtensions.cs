using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bravellian.InfraMonitor.Metrics.AspNetCore;

/// <summary>
/// Endpoint registration helpers for Prometheus scraping.
/// </summary>
public static class BravellianMetricsEndpointExtensions
{
    /// <summary>
    /// Maps the Prometheus scraping endpoint on endpoint routing.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The convention builder or null when the exporter is disabled.</returns>
    public static IEndpointConventionBuilder? MapBravellianMetricsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<BravellianMetricsOptions>();
        if (!options.EnablePrometheusExporter)
        {
            return null;
        }

        return endpoints.MapPrometheusScrapingEndpoint(options.PrometheusEndpointPath);
    }

    /// <summary>
    /// Registers the Prometheus scraping endpoint in the middleware pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseBravellianMetricsEndpoint(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<BravellianMetricsOptions>();
        if (!options.EnablePrometheusExporter)
        {
            return app;
        }

        return app.UseOpenTelemetryPrometheusScrapingEndpoint(options.PrometheusEndpointPath);
    }
}
