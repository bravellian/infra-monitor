using Bravellian.InfraMonitor.Metrics.HttpServer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bravellian.InfraMonitor.Metrics.Tests;

[TestClass]
public sealed class HttpServerMetricsTests
{
    [TestMethod]
    public void Options_Defaults_AreInitialized()
    {
        var options = new BravellianMetricsHttpServerOptions();

        Assert.IsTrue(options.EnableProcessInstrumentation);
        Assert.IsTrue(options.EnableRuntimeInstrumentation);
        Assert.AreEqual("/metrics", options.ScrapeEndpointPath);
        Assert.HasCount(1, options.UriPrefixes);
        Assert.AreEqual("Bravellian.InfraMonitor", options.Meter.MeterName);
        Assert.IsNull(options.Meter.MeterVersion);
    }
}
