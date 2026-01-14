using System;
using System.Threading;
using System.Threading.Tasks;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Bravellian.InfraMonitor.Models;
using Bravellian.InfraMonitor.Services.Setup;
using Bravellian.InfraMonitor.Services.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Bravellian.InfraMonitor.Pages.SqlServer;

public class IndexModel : PageModel
{
    private readonly ISetupStore setupStore;
    private readonly SqlServerReporter reporter;
    private readonly SqlSnapshotStore snapshotStore;
    private readonly ILogger<IndexModel> logger;
    private readonly IWebHostEnvironment environment;

    public IndexModel(
        ISetupStore setupStore,
        SqlServerReporter reporter,
        SqlSnapshotStore snapshotStore,
        ILogger<IndexModel> logger,
        IWebHostEnvironment environment)
    {
        this.setupStore = setupStore;
        this.reporter = reporter;
        this.snapshotStore = snapshotStore;
        this.logger = logger;
        this.environment = environment;
    }

    public bool HasStoredConnection { get; private set; }
    public SqlServerOverviewViewModel? Overview { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorDetail { get; private set; }
    public bool ShowErrorDetail => environment.IsDevelopment();
    public SetupStorageMode StorageMode => setupStore.Mode;
    public IReadOnlyList<SqlSnapshotPoint> SizeHistory { get; private set; } = [];
    public DateTimeOffset? LastSnapshotAt { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        HasStoredConnection = setupStore.TryGetSqlConnectionString(HttpContext, out var connectionString);
        if (!HasStoredConnection)
        {
            return Page();
        }

        try
        {
            Overview = await reporter.GetServerOverviewAsync(connectionString, cancellationToken);
            if (StorageMode == SetupStorageMode.Server)
            {
                SizeHistory = await snapshotStore.GetServerSizeHistoryAsync(30, cancellationToken);
                LastSnapshotAt = await snapshotStore.GetLastServerSnapshotAsync(cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = "Unable to connect to SQL Server with the saved connection string.";
            ErrorDetail = ex.Message;
            logger.LogError(ex, "Failed to load SQL Server overview.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        return await OnGetAsync(cancellationToken);
    }
}
