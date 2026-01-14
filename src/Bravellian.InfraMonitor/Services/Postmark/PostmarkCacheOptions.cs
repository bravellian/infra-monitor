namespace Bravellian.InfraMonitor.Services.Postmark;

public sealed class PostmarkCacheOptions
{
    public int TtlMinutes { get; init; } = 15;
}
