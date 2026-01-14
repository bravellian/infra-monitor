namespace Bravellian.InfraMonitor.Models;

public sealed record SqlServerIndexUsage(
    string SchemaName,
    string TableName,
    string IndexName,
    long UserUpdates,
    double TotalMb);
