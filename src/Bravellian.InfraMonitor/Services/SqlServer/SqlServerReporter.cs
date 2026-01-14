using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Bravellian.InfraMonitor.Models;
using Microsoft.Data.SqlClient;

namespace Bravellian.InfraMonitor.Services.SqlServer;

public sealed class SqlServerReporter
{
    public async Task<SqlServerOverviewViewModel> GetServerOverviewAsync(string connectionString, CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(connectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var serverInfo = await GetServerInfoAsync(connection, cancellationToken).ConfigureAwait(false);
            var databases = await GetDatabaseNamesAsync(connection, cancellationToken).ConfigureAwait(false);
            var topDatabases = await GetTopDatabasesAsync(connection, cancellationToken).ConfigureAwait(false);
            var totalDatabaseSize = await GetTotalDatabaseSizeAsync(connection, cancellationToken).ConfigureAwait(false);
            var topWaits = await GetTopWaitsAsync(connection, cancellationToken).ConfigureAwait(false);
            var topQueriesByCpu = await GetTopQueriesAsync(connection, cancellationToken, orderByReads: false, databaseName: null).ConfigureAwait(false);
            var topQueriesByReads = await GetTopQueriesAsync(connection, cancellationToken, orderByReads: true, databaseName: null).ConfigureAwait(false);

            return new SqlServerOverviewViewModel
            {
                CapturedAt = DateTimeOffset.UtcNow,
                ServerName = serverInfo.ServerName,
                MachineName = serverInfo.MachineName,
                Edition = serverInfo.Edition,
                ProductVersion = serverInfo.ProductVersion,
                ProductLevel = serverInfo.ProductLevel,
                EngineEdition = serverInfo.EngineEdition,
                StartTime = serverInfo.StartTime,
                TotalDatabaseSizeMb = totalDatabaseSize,
                DatabaseNames = databases,
                TopDatabases = topDatabases,
                TopWaits = topWaits,
                TopQueriesByCpu = topQueriesByCpu,
                TopQueriesByReads = topQueriesByReads
            };
        }
    }

    public async Task<SqlServerDatabaseViewModel> GetDatabaseDetailAsync(
        string connectionString,
        string databaseName,
        CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = databaseName
        };

        var connection = new SqlConnection(builder.ConnectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var topTables = await GetTopTablesAsync(connection, cancellationToken).ConfigureAwait(false);
            var topIndexes = await GetTopIndexesAsync(connection, cancellationToken).ConfigureAwait(false);
            var unusedIndexes = await GetUnusedIndexesAsync(connection, cancellationToken).ConfigureAwait(false);
            var missingIndexes = await GetMissingIndexesAsync(connection, cancellationToken).ConfigureAwait(false);
            var databaseSize = await GetDatabaseSizeAsync(connection, cancellationToken).ConfigureAwait(false);
            var topQueriesByCpu = await GetTopQueriesAsync(connection, cancellationToken, orderByReads: false, databaseName).ConfigureAwait(false);
            var topQueriesByReads = await GetTopQueriesAsync(connection, cancellationToken, orderByReads: true, databaseName).ConfigureAwait(false);

            return new SqlServerDatabaseViewModel
            {
                CapturedAt = DateTimeOffset.UtcNow,
                DatabaseName = databaseName,
                DatabaseSizeMb = databaseSize,
                TopTables = topTables,
                TopIndexes = topIndexes,
                UnusedIndexes = unusedIndexes,
                MissingIndexes = missingIndexes,
                TopQueriesByCpu = topQueriesByCpu,
                TopQueriesByReads = topQueriesByReads
            };
        }
    }

    private static async Task<SqlServerServerInfo> GetServerInfoAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string Sql = """
            SELECT
                @@SERVERNAME AS ServerName,
                CAST(SERVERPROPERTY('MachineName') AS NVARCHAR(256)) AS MachineName,
                CAST(SERVERPROPERTY('Edition') AS NVARCHAR(256)) AS Edition,
                CAST(SERVERPROPERTY('ProductVersion') AS NVARCHAR(64)) AS ProductVersion,
                CAST(SERVERPROPERTY('ProductLevel') AS NVARCHAR(64)) AS ProductLevel,
                CAST(SERVERPROPERTY('EngineEdition') AS NVARCHAR(64)) AS EngineEdition,
                sqlserver_start_time AS StartTime
            FROM sys.dm_os_sys_info;
            """;

        var command = new SqlCommand(Sql, connection) { CommandType = CommandType.Text };
        await using (command.ConfigureAwait(false))
        {
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return new SqlServerServerInfo();
                }

                return new SqlServerServerInfo
                {
                    ServerName = reader.GetString(0),
                    MachineName = reader.GetString(1),
                    Edition = reader.GetString(2),
                    ProductVersion = reader.GetString(3),
                    ProductLevel = reader.GetString(4),
                    EngineEdition = reader.GetString(5),
                    StartTime = await reader.IsDBNullAsync(6).ConfigureAwait(false) ? null : reader.GetDateTime(6)
                };
            }
        }
    }

    private static async Task<IReadOnlyList<string>> GetDatabaseNamesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string Sql = """
            SELECT name
            FROM sys.databases
            WHERE state_desc = 'ONLINE'
            ORDER BY name;
            """;

        var results = new List<string>();
        var command = new SqlCommand(Sql, connection);
        await using (command.ConfigureAwait(false))
        {
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    results.Add(reader.GetString(0));
                }

                return results;
            }
        }
    }

    private static async Task<IReadOnlyList<SqlServerDatabaseSize>> GetTopDatabasesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string Sql = """
            SELECT TOP (10)
                d.name,
                SUM(mf.size) * 8.0 / 1024.0 AS SizeMb
            FROM sys.databases d
            INNER JOIN sys.master_files mf ON d.database_id = mf.database_id
            GROUP BY d.name
            ORDER BY SizeMb DESC;
            """;

        var results = new List<SqlServerDatabaseSize>();
        var command = new SqlCommand(Sql, connection);
        await using (command.ConfigureAwait(false))
        {
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    results.Add(new SqlServerDatabaseSize(
                        reader.GetString(0),
                        Convert.ToDouble(reader.GetValue(1), CultureInfo.InvariantCulture)));
                }

                return results;
            }
        }
    }

    private static async Task<double> GetTotalDatabaseSizeAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string Sql = """
            SELECT SUM(size) * 8.0 / 1024.0 AS SizeMb
            FROM sys.master_files;
            """;

        var command = new SqlCommand(Sql, connection);
        await using (command.ConfigureAwait(false))
        {
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is null || result is DBNull ? 0 : Convert.ToDouble(result, CultureInfo.InvariantCulture);
        }
    }

    private static async Task<IReadOnlyList<SqlServerWaitStat>> GetTopWaitsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string Sql = """
            SELECT TOP (10)
                wait_type,
                wait_time_ms,
                signal_wait_time_ms
            FROM sys.dm_os_wait_stats
            WHERE wait_type NOT IN (
                'CLR_SEMAPHORE','LAZYWRITER_SLEEP','RESOURCE_QUEUE','SLEEP_TASK','SLEEP_SYSTEMTASK','SQLTRACE_BUFFER_FLUSH',
                'WAITFOR','LOGMGR_QUEUE','CHECKPOINT_QUEUE','REQUEST_FOR_DEADLOCK_SEARCH','XE_TIMER_EVENT','BROKER_TO_FLUSH',
                'BROKER_TASK_STOP','CLR_MANUAL_EVENT','CLR_AUTO_EVENT','DISPATCHER_QUEUE_SEMAPHORE','FT_IFTS_SCHEDULER_IDLE_WAIT',
                'XE_DISPATCHER_WAIT','XE_DISPATCHER_JOIN','BROKER_EVENTHANDLER','TRACEWRITE','SLEEP_BPOOL_FLUSH','SQLTRACE_INCREMENTAL_FLUSH_SLEEP'
            )
            ORDER BY wait_time_ms DESC;
            """;

        var results = new List<SqlServerWaitStat>();
        var command = new SqlCommand(Sql, connection);
        await using (command.ConfigureAwait(false))
        {
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    results.Add(new SqlServerWaitStat(
                        reader.GetString(0),
                        Convert.ToDouble(reader.GetValue(1), CultureInfo.InvariantCulture),
                        Convert.ToDouble(reader.GetValue(2), CultureInfo.InvariantCulture),
                        0));
                }

                var total = 0d;
                foreach (var wait in results)
                {
                    total += wait.WaitMs;
                }

                if (total <= 0)
                {
                    return results;
                }

                var withPercentage = new List<SqlServerWaitStat>(results.Count);
                foreach (var wait in results)
                {
                    withPercentage.Add(wait with { Percentage = wait.WaitMs / total * 100 });
                }

                return withPercentage;
            }
        }
    }

    private static async Task<IReadOnlyList<SqlServerQueryStat>> GetTopQueriesAsync(
        SqlConnection connection,
        CancellationToken cancellationToken,
        bool orderByReads,
        string? databaseName)
    {
        var orderClause = orderByReads ? "qs.total_logical_reads DESC" : "qs.total_worker_time DESC";
        var sql = $"""
            SELECT TOP (10)
                DB_NAME(st.dbid) AS DatabaseName,
                qs.total_worker_time / 1000.0 AS TotalCpuMs,
                qs.total_logical_reads AS TotalReads,
                qs.execution_count AS ExecutionCount,
                (qs.total_worker_time / 1000.0) / NULLIF(qs.execution_count, 0) AS AvgCpuMs,
                qs.total_logical_reads / NULLIF(qs.execution_count, 0) AS AvgReads,
                SUBSTRING(
                    st.text,
                    (qs.statement_start_offset / 2) + 1,
                    ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(st.text) ELSE qs.statement_end_offset END - qs.statement_start_offset) / 2) + 1
                ) AS QueryText
            FROM sys.dm_exec_query_stats qs
            CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
            WHERE (@DatabaseName IS NULL OR DB_NAME(st.dbid) = @DatabaseName)
            ORDER BY {orderClause};
            """;

        var results = new List<SqlServerQueryStat>();
        var command = new SqlCommand(sql, connection);
        await using (command.ConfigureAwait(false))
        {
            command.Parameters.AddWithValue("@DatabaseName", (object?)databaseName ?? DBNull.Value);
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    results.Add(new SqlServerQueryStat(
                        await reader.IsDBNullAsync(0).ConfigureAwait(false) ? "Unknown" : reader.GetString(0),
                        await reader.IsDBNullAsync(6).ConfigureAwait(false) ? string.Empty : reader.GetString(6).Trim(),
                        await reader.IsDBNullAsync(1).ConfigureAwait(false) ? 0 : Convert.ToDouble(reader.GetValue(1), CultureInfo.InvariantCulture),
                        await reader.IsDBNullAsync(2).ConfigureAwait(false) ? 0 : reader.GetInt64(2),
                        await reader.IsDBNullAsync(3).ConfigureAwait(false) ? 0 : reader.GetInt64(3),
                        await reader.IsDBNullAsync(4).ConfigureAwait(false) ? 0 : Convert.ToDouble(reader.GetValue(4), CultureInfo.InvariantCulture),
                        await reader.IsDBNullAsync(5).ConfigureAwait(false) ? 0 : Convert.ToDouble(reader.GetValue(5), CultureInfo.InvariantCulture)));
                }

                return results;
            }
        }
    }

    private static async Task<IReadOnlyList<SqlServerTableSize>> GetTopTablesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string Sql = """
            SELECT TOP (20)
                s.name AS SchemaName,
                t.name AS TableName,
                SUM(ps.row_count) AS [RowCount],
                SUM(ps.reserved_page_count) * 8.0 / 1024.0 AS TotalMb,
                SUM(ps.used_page_count) * 8.0 / 1024.0 AS UsedMb,
                SUM(ps.in_row_data_page_count + ps.lob_used_page_count + ps.row_overflow_used_page_count) * 8.0 / 1024.0 AS DataMb,
                (SUM(ps.used_page_count) - SUM(ps.in_row_data_page_count + ps.lob_used_page_count + ps.row_overflow_used_page_count)) * 8.0 / 1024.0 AS IndexMb,
                CASE WHEN SUM(ps.row_count) = 0 THEN 0
                     ELSE (SUM(ps.used_page_count) * 8.0) / SUM(ps.row_count) END AS [AvgRowKb]
            FROM sys.dm_db_partition_stats ps
            INNER JOIN sys.tables t ON ps.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE t.is_ms_shipped = 0
            GROUP BY s.name, t.name
            ORDER BY TotalMb DESC;
            """;

        var results = new List<SqlServerTableSize>();
        var command = new SqlCommand(Sql, connection);
        await using (command.ConfigureAwait(false))
        {
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    results.Add(new SqlServerTableSize(
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetInt64(2),
                        Convert.ToDouble(reader.GetValue(3), CultureInfo.InvariantCulture),
                        Convert.ToDouble(reader.GetValue(5), CultureInfo.InvariantCulture),
                        Convert.ToDouble(reader.GetValue(6), CultureInfo.InvariantCulture),
                        Convert.ToDouble(reader.GetValue(7), CultureInfo.InvariantCulture)));
                }

                return results;
            }
        }
    }

    private static async Task<double> GetDatabaseSizeAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string Sql = """
            SELECT SUM(size) * 8.0 / 1024.0 AS SizeMb
            FROM sys.database_files;
            """;

        var command = new SqlCommand(Sql, connection);
        await using (command.ConfigureAwait(false))
        {
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is null || result is DBNull ? 0 : Convert.ToDouble(result, CultureInfo.InvariantCulture);
        }
    }

    private static async Task<IReadOnlyList<SqlServerIndexSize>> GetTopIndexesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string Sql = """
            SELECT TOP (20)
                s.name AS SchemaName,
                t.name AS TableName,
                i.name AS IndexName,
                i.type_desc AS IndexType,
                SUM(ps.used_page_count) * 8.0 / 1024.0 AS TotalMb
            FROM sys.indexes i
            INNER JOIN sys.dm_db_partition_stats ps ON i.object_id = ps.object_id AND i.index_id = ps.index_id
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE i.index_id > 0 AND t.is_ms_shipped = 0
            GROUP BY s.name, t.name, i.name, i.type_desc
            ORDER BY TotalMb DESC;
            """;

        var results = new List<SqlServerIndexSize>();
        var command = new SqlCommand(Sql, connection);
        await using (command.ConfigureAwait(false))
        {
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    results.Add(new SqlServerIndexSize(
                        reader.GetString(0),
                        reader.GetString(1),
                        await reader.IsDBNullAsync(2).ConfigureAwait(false) ? "(heap)" : reader.GetString(2),
                        reader.GetString(3),
                        Convert.ToDouble(reader.GetValue(4), CultureInfo.InvariantCulture)));
                }

                return results;
            }
        }
    }

    private static async Task<IReadOnlyList<SqlServerIndexUsage>> GetUnusedIndexesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string Sql = """
            SELECT TOP (20)
                s.name AS SchemaName,
                t.name AS TableName,
                i.name AS IndexName,
                COALESCE(us.user_updates, 0) AS UserUpdates,
                SUM(ps.used_page_count) * 8.0 / 1024.0 AS TotalMb
            FROM sys.indexes i
            INNER JOIN sys.dm_db_partition_stats ps ON i.object_id = ps.object_id AND i.index_id = ps.index_id
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            LEFT JOIN sys.dm_db_index_usage_stats us
                ON us.object_id = i.object_id AND us.index_id = i.index_id AND us.database_id = DB_ID()
            WHERE i.index_id > 0
              AND t.is_ms_shipped = 0
              AND COALESCE(us.user_seeks, 0) + COALESCE(us.user_scans, 0) + COALESCE(us.user_lookups, 0) = 0
              AND COALESCE(us.user_updates, 0) > 0
            GROUP BY s.name, t.name, i.name, us.user_updates
            ORDER BY TotalMb DESC;
            """;

        var results = new List<SqlServerIndexUsage>();
        var command = new SqlCommand(Sql, connection);
        await using (command.ConfigureAwait(false))
        {
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    results.Add(new SqlServerIndexUsage(
                        reader.GetString(0),
                        reader.GetString(1),
                        await reader.IsDBNullAsync(2).ConfigureAwait(false) ? "(heap)" : reader.GetString(2),
                        reader.GetInt64(3),
                        Convert.ToDouble(reader.GetValue(4), CultureInfo.InvariantCulture)));
                }

                return results;
            }
        }
    }

    private static async Task<IReadOnlyList<SqlServerMissingIndex>> GetMissingIndexesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string Sql = """
            SELECT TOP (20)
                mid.statement AS TableName,
                COALESCE(mid.equality_columns, '') AS EqualityColumns,
                COALESCE(mid.inequality_columns, '') AS InequalityColumns,
                COALESCE(mid.included_columns, '') AS IncludedColumns,
                migs.avg_user_impact,
                migs.user_seeks,
                migs.user_scans,
                migs.avg_total_user_cost
            FROM sys.dm_db_missing_index_details mid
            INNER JOIN sys.dm_db_missing_index_groups mig ON mid.index_handle = mig.index_handle
            INNER JOIN sys.dm_db_missing_index_group_stats migs ON mig.index_group_handle = migs.group_handle
            WHERE mid.database_id = DB_ID()
            ORDER BY (migs.avg_user_impact * (migs.user_seeks + migs.user_scans)) DESC;
            """;

        var results = new List<SqlServerMissingIndex>();
        var command = new SqlCommand(Sql, connection);
        await using (command.ConfigureAwait(false))
        {
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    results.Add(new SqlServerMissingIndex(
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3),
                        await reader.IsDBNullAsync(4).ConfigureAwait(false) ? 0 : Convert.ToDouble(reader.GetValue(4), CultureInfo.InvariantCulture),
                        await reader.IsDBNullAsync(5).ConfigureAwait(false) ? 0 : reader.GetInt64(5),
                        await reader.IsDBNullAsync(6).ConfigureAwait(false) ? 0 : reader.GetInt64(6),
                        await reader.IsDBNullAsync(7).ConfigureAwait(false) ? 0 : Convert.ToDouble(reader.GetValue(7), CultureInfo.InvariantCulture)));
                }

                return results;
            }
        }
    }

    private sealed class SqlServerServerInfo
    {
        public string ServerName { get; init; } = string.Empty;
        public string MachineName { get; init; } = string.Empty;
        public string Edition { get; init; } = string.Empty;
        public string ProductVersion { get; init; } = string.Empty;
        public string ProductLevel { get; init; } = string.Empty;
        public string EngineEdition { get; init; } = string.Empty;
        public DateTime? StartTime { get; init; }
    }
}
