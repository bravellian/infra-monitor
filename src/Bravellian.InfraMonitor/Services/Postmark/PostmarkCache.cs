using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Bravellian.InfraMonitor.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Bravellian.InfraMonitor.Services.Postmark;

public sealed class PostmarkCache
{
    private readonly PostmarkCacheOptions options;
    private readonly string cacheRoot;
    private readonly JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web);

    public PostmarkCache(IHostEnvironment environment, IOptions<PostmarkCacheOptions> options)
    {
        this.options = options.Value;
        cacheRoot = Path.Combine(environment.ContentRootPath, "App_Data", "postmark");
        Directory.CreateDirectory(cacheRoot);
    }

    public async Task<IReadOnlyList<PostmarkEmail>> GetOrAddAsync(
        string apiKey,
        DateOnly from,
        DateOnly to,
        Func<CancellationToken, Task<IReadOnlyList<PostmarkEmail>>> fetch,
        CancellationToken cancellationToken)
    {
        var cachePath = GetCachePath(apiKey, from, to);
        var cached = await TryReadAsync(cachePath, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var emails = await fetch(cancellationToken).ConfigureAwait(false);
        await WriteAsync(cachePath, emails, cancellationToken).ConfigureAwait(false);
        return emails;
    }

    private async Task<IReadOnlyList<PostmarkEmail>?> TryReadAsync(string cachePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(cachePath))
        {
            return null;
        }

        try
        {
            var stream = File.OpenRead(cachePath);
            await using (stream.ConfigureAwait(false))
            {
                var entry = await JsonSerializer.DeserializeAsync<PostmarkCacheEntry>(stream, serializerOptions, cancellationToken).ConfigureAwait(false);
                if (entry is null)
                {
                    return null;
                }

                var ttl = TimeSpan.FromMinutes(Math.Max(1, options.TtlMinutes));
                if (DateTimeOffset.UtcNow - entry.FetchedAt <= ttl)
                {
                    return entry.Emails;
                }
            }
        }
        catch
        {
#pragma warning disable ERP022 // Unobserved exception in a generic exception handler
            return null;
#pragma warning restore ERP022 // Unobserved exception in a generic exception handler
        }

        return null;
    }

    private async Task WriteAsync(string cachePath, IReadOnlyList<PostmarkEmail> emails, CancellationToken cancellationToken)
    {
        var entry = new PostmarkCacheEntry
        {
            FetchedAt = DateTimeOffset.UtcNow,
            Emails = [.. emails]
        };

        var stream = File.Create(cachePath);
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(stream, entry, serializerOptions, cancellationToken).ConfigureAwait(false);
        }
    }

    private string GetCachePath(string apiKey, DateOnly from, DateOnly to)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{apiKey}|{from:O}|{to:O}")))
            .ToLowerInvariant();
        return Path.Combine(cacheRoot, $"postmark_{hash}.json");
    }
}
