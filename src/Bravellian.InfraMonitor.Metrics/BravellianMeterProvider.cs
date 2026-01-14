using System;
using System.Diagnostics.Metrics;
namespace Bravellian.InfraMonitor.Metrics;

public sealed class BravellianMeterProvider
{
    public BravellianMeterProvider(IMeterFactory meterFactory, BravellianMeterOptions options)
    {
        Meter = meterFactory.Create(options.MeterName, options.MeterVersion);
    }

    public BravellianMeterProvider(string meterName, string? meterVersion = null)
    {
        Meter = new Meter(meterName, meterVersion);
    }

    public Meter Meter { get; }

    public Counter<long> CreateCounter(string name, string? unit = null, string? description = null)
        => Meter.CreateCounter<long>(name, unit, description);

    public Counter<double> CreateCounterDouble(string name, string? unit = null, string? description = null)
        => Meter.CreateCounter<double>(name, unit, description);

    public UpDownCounter<long> CreateUpDownCounter(string name, string? unit = null, string? description = null)
        => Meter.CreateUpDownCounter<long>(name, unit, description);

    public Histogram<double> CreateHistogram(string name, string? unit = null, string? description = null)
        => Meter.CreateHistogram<double>(name, unit, description);

    public ObservableGauge<long> CreateObservableGauge(string name, Func<long> observe, string? unit = null, string? description = null)
        => Meter.CreateObservableGauge(name, observe, unit, description);

    public ObservableGauge<double> CreateObservableGauge(string name, Func<double> observe, string? unit = null, string? description = null)
        => Meter.CreateObservableGauge(name, observe, unit, description);
}
