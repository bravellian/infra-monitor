using System;

namespace Bravellian.InfraMonitor.Services.SqlServer;

public sealed class SqlSnapshotOptions
{
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(6);
    public int RetentionDays { get; init; } = 14;
}
