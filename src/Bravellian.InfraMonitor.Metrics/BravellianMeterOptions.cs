namespace Bravellian.InfraMonitor.Metrics;

/// <summary>
/// Defines the name and version of the meter used for instrumentation.
/// </summary>
public sealed class BravellianMeterOptions
{
    /// <summary>
    /// Gets the meter name used to register metrics.
    /// </summary>
    public string MeterName { get; init; } = "Bravellian.InfraMonitor";

    /// <summary>
    /// Gets the optional meter version.
    /// </summary>
    public string? MeterVersion { get; init; }
}
