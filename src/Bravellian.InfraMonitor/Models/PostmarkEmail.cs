using System;

namespace Bravellian.InfraMonitor.Models;

public sealed record PostmarkEmail(string Recipient, DateTimeOffset SentAt);
