using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Bravellian.InfraMonitor.Models;
using PostmarkDotNet;

namespace Bravellian.InfraMonitor.Services.Postmark;

public static class PostmarkClient
{
    private const int PageSize = 500;

    public static async Task<IReadOnlyList<PostmarkEmail>> GetOutboundMessagesAsync(
        string apiKey,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var emails = new List<PostmarkEmail>();
        var offset = 0;
        var total = int.MaxValue;
        var client = new PostmarkDotNet.PostmarkClient(apiKey);
        var fromDate = from.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var toDate = to.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        while (offset < total)
        {
            var payload = await client.GetOutboundMessagesAsync(
                offset,
                PageSize,
                recipient: null,
                fromemail: null,
                tag: null,
                subject: null,
                status: OutboundMessageStatus.Sent,
                toDate: toDate,
                fromDate: fromDate,
                metadata: null,
                messagestream: null).ConfigureAwait(false);

            total = payload.TotalCount;
            if (payload.Messages is null || payload.Messages.Count == 0)
            {
                break;
            }

            foreach (var message in payload.Messages)
            {
                if (message.Recipients is null || message.Recipients.Count == 0)
                {
                    continue;
                }

                var receivedAt = new DateTimeOffset(DateTime.SpecifyKind(message.ReceivedAt, DateTimeKind.Utc));
                foreach (var recipient in message.Recipients.SelectMany(PostmarkRecipientParser.ParseRecipients))
                {
                    emails.Add(new PostmarkEmail(recipient, receivedAt));
                }
            }

            offset += payload.Messages.Count;
        }

        return emails;
    }
}
