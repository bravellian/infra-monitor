using System.Diagnostics.Metrics;
using Bravellian.InfraMonitor.Metrics;

namespace Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;

public sealed class InfraMonitorAppMetrics
{
    private readonly Counter<long> demoEmailsSent;

    public InfraMonitorAppMetrics(BravellianMeterProvider meterProvider)
    {
        demoEmailsSent = meterProvider.CreateCounter(
            "bravellian.infra_monitor.demo_emails_sent",
            "{emails}",
            "Demo metric for email sends.");
    }

    public void RecordDemoEmail(int count = 1)
    {
        if (count <= 0)
        {
            return;
        }

        demoEmailsSent.Add(count);
    }
}
