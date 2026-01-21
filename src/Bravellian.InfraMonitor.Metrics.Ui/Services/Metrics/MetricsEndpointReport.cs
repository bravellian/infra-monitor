using System;
using Bravellian.InfraMonitor.Metrics.Ui.Models;

namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

/// <summary>
/// Captures the result of scraping a metrics endpoint.
/// </summary>
/// <param name="Registration">The endpoint registration details.</param>
/// <param name="Snapshot">The parsed scrape snapshot if successful.</param>
/// <param name="Error">The error message when scraping fails.</param>
/// <param name="Duration">The scrape duration.</param>
public sealed record MetricsEndpointReport(
    MetricsEndpointRegistration Registration,
    PrometheusScrapeSnapshot? Snapshot,
    string? Error,
    TimeSpan Duration);
