namespace Bravellian.InfraMonitor.Metrics.Ui.Models;

public sealed record MetricsEndpointRegistration
{
    public string ServiceName { get; init; } = string.Empty;
    public string InstanceName { get; init; } = string.Empty;
    public string EndpointUrl { get; init; } = string.Empty;
}
