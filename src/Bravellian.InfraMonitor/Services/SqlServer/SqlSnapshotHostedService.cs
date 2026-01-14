using System;
using System.Threading;
using System.Threading.Tasks;
using Bravellian.InfraMonitor.Services.Setup;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bravellian.InfraMonitor.Services.SqlServer;

public sealed class SqlSnapshotHostedService : BackgroundService
{
    private readonly ISetupStore setupStore;
    private readonly SqlServerReporter reporter;
    private readonly SqlSnapshotStore store;
    private readonly SqlSnapshotOptions options;
    private readonly ILogger<SqlSnapshotHostedService> logger;

    public SqlSnapshotHostedService(
        ISetupStore setupStore,
        SqlServerReporter reporter,
        SqlSnapshotStore store,
        IOptions<SqlSnapshotOptions> options,
        ILogger<SqlSnapshotHostedService> logger)
    {
        this.setupStore = setupStore;
        this.reporter = reporter;
        this.store = store;
        this.options = options.Value;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (setupStore.Mode != SetupStorageMode.Server)
        {
            logger.LogInformation("SQL snapshotting disabled because SetupStorageMode is not Server.");
            return;
        }

        var timer = new PeriodicTimer(options.Interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CaptureSnapshotsAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to capture SQL snapshots.");
            }

            await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task CaptureSnapshotsAsync(CancellationToken cancellationToken)
    {
        if (!setupStore.TryGetSqlConnectionStringForServer(out var connectionString))
        {
            logger.LogWarning("SQL snapshot skipped because no server-side connection string is configured.");
            return;
        }

        var overview = await reporter.GetServerOverviewAsync(connectionString, cancellationToken).ConfigureAwait(false);
        await store.SaveServerSnapshotAsync(overview, cancellationToken).ConfigureAwait(false);

        foreach (var database in overview.DatabaseNames)
        {
            try
            {
                var detail = await reporter.GetDatabaseDetailAsync(connectionString, database, cancellationToken).ConfigureAwait(false);
                await store.SaveDatabaseSnapshotAsync(database, detail, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to capture snapshot for database {Database}.", database);
            }
        }

        await store.CleanupAsync(cancellationToken).ConfigureAwait(false);
    }
}
