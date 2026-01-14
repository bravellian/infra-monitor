namespace Bravellian.InfraMonitor.Models;

public sealed record SqlServerWaitStat(string WaitType, double WaitMs, double SignalWaitMs, double Percentage);
