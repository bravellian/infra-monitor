using OpenTelemetry.Metrics;

namespace Bravellian.InfraMonitor.Metrics.HttpServer;

internal static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder ConfigureInstrumentation(this MeterProviderBuilder builder, BravellianMetricsHttpServerOptions options)
    {
        if (options.EnableRuntimeInstrumentation)
        {
            builder.AddRuntimeInstrumentation();
        }

        if (options.EnableProcessInstrumentation)
        {
            builder.AddProcessInstrumentation();
        }

        return builder;
    }
}
