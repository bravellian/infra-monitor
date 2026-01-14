using System.Collections.Generic;
using Bravellian.InfraMonitor.Metrics.Ui.Models;

namespace Bravellian.InfraMonitor.Metrics.Ui.Sample;

public sealed class MetricsUiOptions
{
    public IList<MetricsEndpointRegistration> Endpoints { get; init; } = new List<MetricsEndpointRegistration>();
    public IList<string> PinnedMetrics { get; init; } = new List<string>();
    public int RefreshSeconds { get; init; } = 20;
}
