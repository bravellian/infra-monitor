using System.Collections.Generic;

namespace Bravellian.InfraMonitor.Models;

public sealed class PostmarkReportViewModel
{
    public IReadOnlyList<string> DateLabels { get; init; } = [];
    public IReadOnlyList<int> DailyTotals { get; init; } = [];
    public IReadOnlyList<string> TopRecipients { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<int>> RecipientDailyCounts { get; init; } = [];
    public int TotalMessages { get; init; }
}
