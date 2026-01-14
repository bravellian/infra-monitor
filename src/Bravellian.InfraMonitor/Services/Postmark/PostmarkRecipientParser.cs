using System;
using System.Collections.Generic;
using System.Net.Mail;

namespace Bravellian.InfraMonitor.Services.Postmark;

public static class PostmarkRecipientParser
{
    public static IEnumerable<string> ParseRecipients(string toField)
    {
        if (string.IsNullOrWhiteSpace(toField))
        {
            yield break;
        }

        var segments = toField.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            var normalized = NormalizeRecipient(segment);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                yield return normalized;
            }
        }
    }

    private static string? NormalizeRecipient(string value)
    {
        try
        {
            return new MailAddress(value).Address.ToLowerInvariant();
        }
        catch
        {
#pragma warning disable ERP022 // Unobserved exception in a generic exception handler
            return value.Trim().ToLowerInvariant();
#pragma warning restore ERP022 // Unobserved exception in a generic exception handler
        }
    }
}
