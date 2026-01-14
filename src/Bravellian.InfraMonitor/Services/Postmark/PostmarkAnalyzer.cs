using System;
using System.Collections.Generic;
using System.Linq;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Bravellian.InfraMonitor.Models;

namespace Bravellian.InfraMonitor.Services.Postmark;

public sealed class PostmarkAnalyzer
{
    public PostmarkReportViewModel BuildReport(IEnumerable<PostmarkEmail> emails, DateOnly from, DateOnly to)
    {
        var dateLabels = new List<string>();
        var dates = new List<DateOnly>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            dates.Add(date);
            dateLabels.Add(date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        }

        var dateIndex = dates
            .Select((value, index) => new { value, index })
            .ToDictionary(x => x.value, x => x.index);

        var dailyTotals = new int[dates.Count];
        var recipientTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var recipientDaily = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase);
        var totalMessages = 0;

        foreach (var email in emails)
        {
            var localDate = DateOnly.FromDateTime(email.SentAt.LocalDateTime);
            if (!dateIndex.TryGetValue(localDate, out var index))
            {
                continue;
            }

            dailyTotals[index]++;
            totalMessages++;

            var recipient = email.Recipient;
            if (!recipientTotals.TryGetValue(recipient, out var current))
            {
                recipientTotals[recipient] = 1;
                recipientDaily[recipient] = new int[dates.Count];
            }
            else
            {
                recipientTotals[recipient] = current + 1;
            }

            recipientDaily[recipient][index]++;
        }

        var topRecipients = recipientTotals
            .OrderByDescending(pair => pair.Value)
            .Take(20)
            .Select(pair => pair.Key)
            .ToList();

        var recipientDailyCounts = topRecipients
            .Select(recipient => (IReadOnlyList<int>)recipientDaily[recipient])
            .ToList();

        return new PostmarkReportViewModel
        {
            DateLabels = dateLabels,
            DailyTotals = dailyTotals,
            TopRecipients = topRecipients,
            RecipientDailyCounts = recipientDailyCounts,
            TotalMessages = totalMessages
        };
    }
}
