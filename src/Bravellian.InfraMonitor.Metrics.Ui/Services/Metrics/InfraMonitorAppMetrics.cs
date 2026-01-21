using System.Diagnostics.Metrics;
using Bravellian.InfraMonitor.Metrics;

namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

/// <summary>
/// Provides application-level metrics for the metrics UI sample workflows.
/// </summary>
public sealed class InfraMonitorAppMetrics
{
    private readonly Counter<long> demoEmailsSent;

    /// <summary>
    /// Initializes the application metrics using the provided meter provider.
    /// </summary>
    /// <param name="meterProvider">The meter provider used to create instruments.</param>
    public InfraMonitorAppMetrics(BravellianMeterProvider meterProvider)
    {
        demoEmailsSent = meterProvider.CreateCounter(
            "bravellian.infra_monitor.demo_emails_sent",
            "{emails}",
            "Demo metric for email sends.");
    }

    /// <summary>
    /// Records a demo email count.
    /// </summary>
    /// <param name="count">The number of demo emails.</param>
    public void RecordDemoEmail(int count = 1)
    {
        if (count <= 0)
        {
            return;
        }

        demoEmailsSent.Add(count);
    }
}
