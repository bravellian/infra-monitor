namespace Bravellian.InfraMonitor.Metrics.Ui.Models;

/// <summary>
/// Describes a Prometheus metrics endpoint exposed by a service instance.
/// </summary>
public sealed record MetricsEndpointRegistration
{
    /// <summary>
    /// Gets the logical service name.
    /// </summary>
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the instance name of the service.
    /// </summary>
    public string InstanceName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the endpoint URL used for scraping metrics.
    /// </summary>
    public string EndpointUrl { get; init; } = string.Empty;
}
