using System.Diagnostics.Metrics;
using Bravellian.InfraMonitor.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bravellian.InfraMonitor.Metrics.Tests;

[TestClass]
public sealed class MetricsTests
{
    [TestMethod]
    public void MeterOptions_HaveExpectedDefaults()
    {
        var options = new BravellianMeterOptions();

        Assert.AreEqual("Bravellian.InfraMonitor", options.MeterName);
        Assert.IsNull(options.MeterVersion);
    }

    [TestMethod]
    public void MeterProvider_CreatesInstruments()
    {
        var provider = new BravellianMeterProvider("Test.Meter", "1.0");

        Counter<long> counter = provider.CreateCounter("test.counter");
        Counter<double> doubleCounter = provider.CreateCounterDouble("test.double.counter");
        UpDownCounter<long> upDown = provider.CreateUpDownCounter("test.updown");
        Histogram<double> histogram = provider.CreateHistogram("test.histogram");
        ObservableGauge<long> gauge = provider.CreateObservableGauge("test.gauge", () => 1);

        Assert.IsNotNull(counter);
        Assert.IsNotNull(doubleCounter);
        Assert.IsNotNull(upDown);
        Assert.IsNotNull(histogram);
        Assert.IsNotNull(gauge);
    }

}
