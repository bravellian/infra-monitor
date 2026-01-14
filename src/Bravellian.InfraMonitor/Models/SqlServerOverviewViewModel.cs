using System;
using System.Collections.Generic;

namespace Bravellian.InfraMonitor.Models;

public sealed class SqlServerOverviewViewModel
{
    public DateTimeOffset CapturedAt { get; init; }
    public string ServerName { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public string Edition { get; init; } = string.Empty;
    public string ProductVersion { get; init; } = string.Empty;
    public string ProductLevel { get; init; } = string.Empty;
    public string EngineEdition { get; init; } = string.Empty;
    public DateTimeOffset? StartTime { get; init; }
    public double TotalDatabaseSizeMb { get; init; }

    public IReadOnlyList<SqlServerDatabaseSize> TopDatabases { get; init; } = [];
    public IReadOnlyList<SqlServerWaitStat> TopWaits { get; init; } = [];
    public IReadOnlyList<SqlServerQueryStat> TopQueriesByCpu { get; init; } = [];
    public IReadOnlyList<SqlServerQueryStat> TopQueriesByReads { get; init; } = [];
    public IReadOnlyList<string> DatabaseNames { get; init; } = [];
}
