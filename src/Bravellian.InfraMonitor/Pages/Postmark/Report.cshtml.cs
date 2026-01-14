using System;
using System.Threading;
using System.Threading.Tasks;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Bravellian.InfraMonitor.Models;
using Bravellian.InfraMonitor.Services.Postmark;
using Bravellian.InfraMonitor.Services.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bravellian.InfraMonitor.Pages.Postmark;

public class ReportModel : PageModel
{
    private readonly PostmarkCache cache;
    private readonly PostmarkAnalyzer analyzer;
    private readonly ISetupStore setupStore;

    public ReportModel(PostmarkCache cache, PostmarkAnalyzer analyzer, ISetupStore setupStore)
    {
        this.cache = cache;
        this.analyzer = analyzer;
        this.setupStore = setupStore;
    }

    [BindProperty]
    public DateOnly? From { get; set; }

    [BindProperty]
    public DateOnly? To { get; set; }

    public PostmarkReportViewModel? Report { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool HasResults => Report is not null;
    public bool HasStoredToken { get; private set; }

    public void OnGet()
    {
        SetDefaults();
        HasStoredToken = setupStore.TryGetPostmarkToken(HttpContext, out _);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        SetDefaults();
        HasStoredToken = setupStore.TryGetPostmarkToken(HttpContext, out var storedToken);
        var tokenToUse = storedToken;

        if (string.IsNullOrWhiteSpace(tokenToUse))
        {
            ErrorMessage = "Please add a Postmark server token on the setup page.";
            return Page();
        }

        if (From is null || To is null || From > To)
        {
            ErrorMessage = "Please provide a valid date range.";
            return Page();
        }

        try
        {
            var emails = await cache.GetOrAddAsync(
                tokenToUse,
                From.Value,
                To.Value,
                ct => PostmarkClient.GetOutboundMessagesAsync(tokenToUse, From.Value, To.Value, ct),
                cancellationToken);

            Report = analyzer.BuildReport(emails, From.Value, To.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = "Postmark rejected the request. Double-check the server token and try again.";
        }

        return Page();
    }

    private void SetDefaults()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        To ??= today;
        From ??= today.AddDays(-13);
    }
}
