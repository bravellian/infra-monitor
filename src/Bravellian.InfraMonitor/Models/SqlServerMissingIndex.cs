namespace Bravellian.InfraMonitor.Models;

public sealed record SqlServerMissingIndex(
    string TableName,
    string EqualityColumns,
    string InequalityColumns,
    string IncludedColumns,
    double AvgUserImpact,
    long UserSeeks,
    long UserScans,
    double AvgTotalUserCost);
