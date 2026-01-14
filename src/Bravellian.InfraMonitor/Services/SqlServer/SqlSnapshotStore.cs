using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Bravellian.InfraMonitor.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Bravellian.InfraMonitor.Services.SqlServer;

public sealed class SqlSnapshotStore
{
    private readonly string rootPath;
    private readonly SqlSnapshotOptions options;
    private readonly JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim gate = new(1, 1);

    public SqlSnapshotStore(IHostEnvironment environment, IOptions<SqlSnapshotOptions> options)
    {
        this.options = options.Value;
        rootPath = Path.Combine(environment.ContentRootPath, "App_Data", "sqlsnapshots");
        Directory.CreateDirectory(rootPath);
    }

    public async Task SaveServerSnapshotAsync(SqlServerOverviewViewModel snapshot, CancellationToken cancellationToken)
    {
        var path = Path.Combine(rootPath, $"server_{snapshot.CapturedAt:yyyyMMddHHmmss}.json");
        await WriteAsync(path, snapshot, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveDatabaseSnapshotAsync(string databaseName, SqlServerDatabaseViewModel snapshot, CancellationToken cancellationToken)
    {
        var safeName = string.Concat(databaseName.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
        var path = Path.Combine(rootPath, $"db_{safeName}_{snapshot.CapturedAt:yyyyMMddHHmmss}.json");
        await WriteAsync(path, snapshot, cancellationToken).ConfigureAwait(false);
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-Math.Max(1, options.RetentionDays));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var files = Directory.GetFiles(rootPath, "*.json");
            foreach (var file in files)
            {
                var lastWrite = File.GetLastWriteTimeUtc(file);
                if (lastWrite < cutoff.UtcDateTime)
                {
                    File.Delete(file);
                }
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<SqlSnapshotPoint>> GetServerSizeHistoryAsync(int maxPoints, CancellationToken cancellationToken)
    {
        var snapshots = await ReadServerSnapshotsAsync(cancellationToken).ConfigureAwait(false);
        var points = new List<SqlSnapshotPoint>();
        foreach (var snapshot in snapshots)
        {
            points.Add(new SqlSnapshotPoint(snapshot.CapturedAt, snapshot.TotalDatabaseSizeMb));
        }

        return points
            .OrderBy(point => point.CapturedAt)
            .TakeLast(Math.Max(1, maxPoints))
            .ToList();
    }

    public async Task<IReadOnlyList<SqlSnapshotPoint>> GetDatabaseSizeHistoryAsync(
        string databaseName,
        int maxPoints,
        CancellationToken cancellationToken)
    {
        var snapshots = await ReadDatabaseSnapshotsAsync(databaseName, cancellationToken).ConfigureAwait(false);
        var points = new List<SqlSnapshotPoint>();
        foreach (var snapshot in snapshots)
        {
            points.Add(new SqlSnapshotPoint(snapshot.CapturedAt, snapshot.DatabaseSizeMb));
        }

        return points
            .OrderBy(point => point.CapturedAt)
            .TakeLast(Math.Max(1, maxPoints))
            .ToList();
    }

    public async Task<DateTimeOffset?> GetLastServerSnapshotAsync(CancellationToken cancellationToken)
    {
        var snapshots = await ReadServerSnapshotsAsync(cancellationToken).ConfigureAwait(false);
        return snapshots.Count == 0 ? null : snapshots.Max(s => s.CapturedAt);
    }

    public async Task<DateTimeOffset?> GetLastDatabaseSnapshotAsync(string databaseName, CancellationToken cancellationToken)
    {
        var snapshots = await ReadDatabaseSnapshotsAsync(databaseName, cancellationToken).ConfigureAwait(false);
        return snapshots.Count == 0 ? null : snapshots.Max(s => s.CapturedAt);
    }

    private async Task WriteAsync<T>(string path, T payload, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var stream = File.Create(path);
            await using (stream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(stream, payload, serializerOptions, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<List<SqlServerOverviewViewModel>> ReadServerSnapshotsAsync(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles(rootPath, "server_*.json");
        var results = new List<SqlServerOverviewViewModel>();
        foreach (var file in files)
        {
            var snapshot = await ReadSnapshotAsync<SqlServerOverviewViewModel>(file, cancellationToken).ConfigureAwait(false);
            if (snapshot is not null)
            {
                results.Add(snapshot);
            }
        }

        return results;
    }

    private async Task<List<SqlServerDatabaseViewModel>> ReadDatabaseSnapshotsAsync(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var safeName = string.Concat(databaseName.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
        var files = Directory.GetFiles(rootPath, $"db_{safeName}_*.json");
        var results = new List<SqlServerDatabaseViewModel>();
        foreach (var file in files)
        {
            var snapshot = await ReadSnapshotAsync<SqlServerDatabaseViewModel>(file, cancellationToken).ConfigureAwait(false);
            if (snapshot is not null)
            {
                results.Add(snapshot);
            }
        }

        return results;
    }

    private async Task<T?> ReadSnapshotAsync<T>(string path, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var stream = File.OpenRead(path);
            await using (stream.ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync<T>(stream, serializerOptions, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
#pragma warning disable ERP022 // Unobserved exception in a generic exception handler
            return default;
#pragma warning restore ERP022 // Unobserved exception in a generic exception handler
        }
        finally
        {
            gate.Release();
        }
    }
}
