using System;
using Bravellian.InfraMonitor.Metrics.Ui.Models;

namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

public sealed record MetricsEndpointReport(
    MetricsEndpointRegistration Registration,
    PrometheusScrapeSnapshot? Snapshot,
    string? Error,
    TimeSpan Duration);
