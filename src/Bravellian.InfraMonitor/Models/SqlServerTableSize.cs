namespace Bravellian.InfraMonitor.Models;

public sealed record SqlServerTableSize(
    string SchemaName,
    string TableName,
    long RowCount,
    double TotalMb,
    double DataMb,
    double IndexMb,
    double AvgRowKb);
