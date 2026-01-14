namespace Bravellian.InfraMonitor.Models;

public sealed record SqlServerIndexSize(
    string SchemaName,
    string TableName,
    string IndexName,
    string IndexType,
    double TotalMb);
