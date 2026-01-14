using System;
using System.Collections.Generic;

namespace Bravellian.InfraMonitor.Models;

public sealed class SqlServerDatabaseViewModel
{
    public DateTimeOffset CapturedAt { get; init; }
    public string DatabaseName { get; init; } = string.Empty;
    public double DatabaseSizeMb { get; init; }
    public IReadOnlyList<SqlServerTableSize> TopTables { get; init; } = [];
    public IReadOnlyList<SqlServerIndexSize> TopIndexes { get; init; } = [];
    public IReadOnlyList<SqlServerIndexUsage> UnusedIndexes { get; init; } = [];
    public IReadOnlyList<SqlServerMissingIndex> MissingIndexes { get; init; } = [];
    public IReadOnlyList<SqlServerQueryStat> TopQueriesByCpu { get; init; } = [];
    public IReadOnlyList<SqlServerQueryStat> TopQueriesByReads { get; init; } = [];
}
