using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bravellian.InfraMonitor.Metrics.AspNetCore;

public static class BravellianMetricsEndpointExtensions
{
    public static IEndpointConventionBuilder? MapBravellianMetricsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<BravellianMetricsOptions>();
        if (!options.EnablePrometheusExporter)
        {
            return null;
        }

        return endpoints.MapPrometheusScrapingEndpoint(options.PrometheusEndpointPath);
    }

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
