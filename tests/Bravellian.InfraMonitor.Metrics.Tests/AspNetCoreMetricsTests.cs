using System.Linq;
using Bravellian.InfraMonitor.Metrics;
using Bravellian.InfraMonitor.Metrics.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bravellian.InfraMonitor.Metrics.Tests;

[TestClass]
public sealed class AspNetCoreMetricsTests
{
    [TestMethod]
    public void AddBravellianMetrics_RegistersOptionsAndProvider()
    {
        var services = new ServiceCollection();

        services.AddBravellianMetrics();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<BravellianMetricsOptions>();

        Assert.IsFalse(options.EnablePrometheusExporter);
        Assert.IsTrue(services.Any(service => service.ServiceType == typeof(BravellianMeterProvider)));
    }

    [TestMethod]
    public void AddBravellianMetrics_AppliesConfiguration()
    {
        var services = new ServiceCollection();

        services.AddBravellianMetrics(options =>
        {
            options.EnablePrometheusExporter = true;
            options.PrometheusEndpointPath = "/custom-metrics";
            options.PrometheusScrapeResponseCacheMilliseconds = 50;
            options.Meter = new BravellianMeterOptions
            {
                MeterName = "Configured.Meter",
                MeterVersion = "3.0"
            };
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<BravellianMetricsOptions>();

        Assert.IsTrue(options.EnablePrometheusExporter);
        Assert.AreEqual("/custom-metrics", options.PrometheusEndpointPath);
        Assert.AreEqual(50, options.PrometheusScrapeResponseCacheMilliseconds);
        Assert.AreEqual("Configured.Meter", options.Meter.MeterName);
        Assert.AreEqual("3.0", options.Meter.MeterVersion);
    }
}
