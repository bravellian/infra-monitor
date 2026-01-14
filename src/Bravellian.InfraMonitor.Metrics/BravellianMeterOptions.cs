namespace Bravellian.InfraMonitor.Metrics;

public sealed class BravellianMeterOptions
{
    public string MeterName { get; init; } = "Bravellian.InfraMonitor";
    public string? MeterVersion { get; init; }
}
