namespace Bravellian.InfraMonitor.Models;

public sealed record SqlServerQueryStat(
    string DatabaseName,
    string QueryText,
    double TotalCpuMs,
    long TotalReads,
    long ExecutionCount,
    double AvgCpuMs,
    double AvgReads);
