using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Bravellian.InfraMonitor.Services.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bravellian.InfraMonitor.Pages;

public class SetupModel : PageModel
{
    private readonly ISetupStore setupStore;

    public SetupModel(ISetupStore setupStore)
    {
        this.setupStore = setupStore;
    }

    [BindProperty]
    public string? PostmarkToken { get; set; }

    [BindProperty]
    public string? SqlConnectionString { get; set; }

    [BindProperty]
    public string? MetricsServiceName { get; set; }

    [BindProperty]
    public string? MetricsInstanceName { get; set; }

    [BindProperty]
    public string? MetricsEndpointUrl { get; set; }

    [BindProperty]
    public int? MetricsRefreshSeconds { get; set; }

    [BindProperty]
    public string? MetricsPinnedInput { get; set; }

    public bool HasSavedPostmarkToken { get; private set; }
    public bool HasSavedSqlConnection { get; private set; }
    public IReadOnlyList<MetricsEndpointRegistration> MetricsEndpoints { get; private set; } =
        Array.Empty<MetricsEndpointRegistration>();
    public int CurrentMetricsRefreshSeconds { get; private set; } = DefaultMetricsRefreshSeconds;
    public IReadOnlyList<string> PinnedMetrics { get; private set; } = Array.Empty<string>();
    public SetupStorageMode StorageMode => setupStore.Mode;
    private string StorageLabel => StorageMode == SetupStorageMode.Server ? "server" : "this browser";
    private const int DefaultMetricsRefreshSeconds = 20;
    private const int MinMetricsRefreshSeconds = 5;
    private const int MaxMetricsRefreshSeconds = 300;

    public void OnGet()
    {
        LoadState();
    }

    public IActionResult OnPostPostmark()
    {
        LoadState();

        if (string.IsNullOrWhiteSpace(PostmarkToken))
        {
            ModelState.AddModelError(nameof(PostmarkToken), "Please enter a Postmark server token.");
            return Page();
        }

        setupStore.SetPostmarkToken(HttpContext, PostmarkToken!);
        TempData["SetupSaved"] = $"Postmark token saved on {StorageLabel}.";
        return RedirectToPage();
    }

    public IActionResult OnPostPostmarkClear()
    {
        setupStore.ClearPostmarkToken(HttpContext);
        TempData["SetupSaved"] = "Postmark token cleared.";
        return RedirectToPage();
    }

    public IActionResult OnPostSql()
    {
        LoadState();

        if (string.IsNullOrWhiteSpace(SqlConnectionString))
        {
            ModelState.AddModelError(nameof(SqlConnectionString), "Please enter a SQL Server connection string.");
            return Page();
        }

        setupStore.SetSqlConnectionString(HttpContext, SqlConnectionString!);
        TempData["SetupSaved"] = $"SQL connection string saved on {StorageLabel}.";
        return RedirectToPage();
    }

    public IActionResult OnPostSqlClear()
    {
        setupStore.ClearSqlConnectionString(HttpContext);
        TempData["SetupSaved"] = "SQL connection string cleared.";
        return RedirectToPage();
    }

    public IActionResult OnPostMetricsAdd()
    {
        LoadState();

        if (string.IsNullOrWhiteSpace(MetricsServiceName))
        {
            ModelState.AddModelError(nameof(MetricsServiceName), "Please enter a service name.");
        }

        if (string.IsNullOrWhiteSpace(MetricsInstanceName))
        {
            ModelState.AddModelError(nameof(MetricsInstanceName), "Please enter an instance name.");
        }

        if (string.IsNullOrWhiteSpace(MetricsEndpointUrl))
        {
            ModelState.AddModelError(nameof(MetricsEndpointUrl), "Please enter a metrics endpoint URL.");
        }
        else if (!Uri.TryCreate(MetricsEndpointUrl, UriKind.Absolute, out var parsed) ||
                 !(parsed.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                   parsed.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(MetricsEndpointUrl), "Please enter a valid http(s) URL.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var endpoints = MetricsEndpoints.ToList();
        endpoints.Add(new MetricsEndpointRegistration
        {
            ServiceName = MetricsServiceName!.Trim(),
            InstanceName = MetricsInstanceName!.Trim(),
            EndpointUrl = MetricsEndpointUrl!.Trim()
        });

        setupStore.SetMetricsEndpoints(HttpContext, endpoints);
        TempData["SetupSaved"] = $"Metrics endpoint saved on {StorageLabel}.";
        return RedirectToPage();
    }

    public IActionResult OnPostMetricsRemove(int index)
    {
        LoadState();

        var endpoints = MetricsEndpoints.ToList();
        if (index >= 0 && index < endpoints.Count)
        {
            endpoints.RemoveAt(index);
            setupStore.SetMetricsEndpoints(HttpContext, endpoints);
            TempData["SetupSaved"] = "Metrics endpoint removed.";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostMetricsClear()
    {
        setupStore.ClearMetricsEndpoints(HttpContext);
        TempData["SetupSaved"] = "All metrics endpoints cleared.";
        return RedirectToPage();
    }

    public IActionResult OnPostMetricsRefresh()
    {
        LoadState();

        if (!MetricsRefreshSeconds.HasValue)
        {
            ModelState.AddModelError(nameof(MetricsRefreshSeconds), "Please enter a refresh interval in seconds.");
            return Page();
        }

        var refreshSeconds = MetricsRefreshSeconds.Value;
        if (refreshSeconds != 0 && (refreshSeconds < MinMetricsRefreshSeconds || refreshSeconds > MaxMetricsRefreshSeconds))
        {
            ModelState.AddModelError(
                nameof(MetricsRefreshSeconds),
                $"Please enter 0 to disable or a value between {MinMetricsRefreshSeconds} and {MaxMetricsRefreshSeconds} seconds.");
            return Page();
        }

        setupStore.SetMetricsRefreshSeconds(HttpContext, refreshSeconds);
        TempData["SetupSaved"] = $"Metrics refresh interval saved on {StorageLabel}.";
        return RedirectToPage();
    }

    public IActionResult OnPostMetricsRefreshClear()
    {
        setupStore.ClearMetricsRefreshSeconds(HttpContext);
        TempData["SetupSaved"] = "Metrics refresh interval reset to default.";
        return RedirectToPage();
    }

    public IActionResult OnPostMetricsPinned()
    {
        LoadState();

        var parsed = ParsePinnedMetrics(MetricsPinnedInput);
        if (parsed.Count == 0)
        {
            ModelState.AddModelError(nameof(MetricsPinnedInput), "Please enter at least one metric name.");
            return Page();
        }

        setupStore.SetPinnedMetrics(HttpContext, parsed);
        TempData["SetupSaved"] = $"Pinned metrics saved on {StorageLabel}.";
        return RedirectToPage();
    }

    public IActionResult OnPostMetricsPinnedClear()
    {
        setupStore.ClearPinnedMetrics(HttpContext);
        TempData["SetupSaved"] = "Pinned metrics cleared.";
        return RedirectToPage();
    }

    private void LoadState()
    {
        HasSavedPostmarkToken = setupStore.TryGetPostmarkToken(HttpContext, out _);
        HasSavedSqlConnection = setupStore.TryGetSqlConnectionString(HttpContext, out _);
        if (!setupStore.TryGetMetricsEndpoints(HttpContext, out var endpoints))
        {
            endpoints = Array.Empty<MetricsEndpointRegistration>();
        }

        MetricsEndpoints = endpoints;
        CurrentMetricsRefreshSeconds = DefaultMetricsRefreshSeconds;
        if (setupStore.TryGetMetricsRefreshSeconds(HttpContext, out var refreshSeconds))
        {
            CurrentMetricsRefreshSeconds = refreshSeconds;
        }

        if (!setupStore.TryGetPinnedMetrics(HttpContext, out var pinned))
        {
            pinned = Array.Empty<string>();
        }

        PinnedMetrics = pinned;
        MetricsPinnedInput = string.Join(Environment.NewLine, PinnedMetrics);
    }

    private static IReadOnlyList<string> ParsePinnedMetrics(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<string>();
        }

        var items = input
            .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return items;
    }
}
