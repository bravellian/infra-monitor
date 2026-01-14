using System;
using System.Collections.Generic;

namespace Bravellian.InfraMonitor.Models;

public sealed class PostmarkCacheEntry
{
    public DateTimeOffset FetchedAt { get; init; }
    public IReadOnlyList<PostmarkEmail> Emails { get; init; } = new List<PostmarkEmail>();
}
