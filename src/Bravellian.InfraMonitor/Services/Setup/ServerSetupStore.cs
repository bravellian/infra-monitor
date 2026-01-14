using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Bravellian.InfraMonitor.Services.Setup;

public sealed class ServerSetupStore : ISetupStore
{
    private readonly IDataProtector protector;
    private readonly string storePath;
    private readonly SemaphoreSlim @lock = new(1, 1);
    private readonly JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web);

    public ServerSetupStore(IDataProtectionProvider provider, IHostEnvironment environment)
    {
        protector = provider.CreateProtector("Bravellian.InfraMonitor.Setup.ServerStore");
        storePath = Path.Combine(environment.ContentRootPath, "App_Data", "setup.json");
        Directory.CreateDirectory(Path.GetDirectoryName(storePath)!);
    }

    public SetupStorageMode Mode => SetupStorageMode.Server;

    public bool TryGetPostmarkToken(HttpContext context, out string token)
        => TryGetValue(data => data.PostmarkToken, out token);

    public bool TryGetSqlConnectionString(HttpContext context, out string connectionString)
        => TryGetValue(data => data.SqlConnectionString, out connectionString);

    public bool TryGetMetricsEndpoints(HttpContext context, out IReadOnlyList<MetricsEndpointRegistration> endpoints)
        => TryGetMetricsValue(data => data.MetricsEndpoints, out endpoints);

    public bool TryGetMetricsRefreshSeconds(HttpContext context, out int refreshSeconds)
        => TryGetIntValue(data => data.MetricsRefreshSeconds, out refreshSeconds);

    public bool TryGetPinnedMetrics(HttpContext context, out IReadOnlyList<string> metricNames)
        => TryGetStringListValue(data => data.MetricsPinnedMetrics, out metricNames);

    public bool TryGetPostmarkTokenForServer(out string token)
        => TryGetValue(data => data.PostmarkToken, out token);

    public bool TryGetSqlConnectionStringForServer(out string connectionString)
        => TryGetValue(data => data.SqlConnectionString, out connectionString);

    public bool TryGetMetricsEndpointsForServer(out IReadOnlyList<MetricsEndpointRegistration> endpoints)
        => TryGetMetricsValue(data => data.MetricsEndpoints, out endpoints);

    public bool TryGetMetricsRefreshSecondsForServer(out int refreshSeconds)
        => TryGetIntValue(data => data.MetricsRefreshSeconds, out refreshSeconds);

    public bool TryGetPinnedMetricsForServer(out IReadOnlyList<string> metricNames)
        => TryGetStringListValue(data => data.MetricsPinnedMetrics, out metricNames);

    public void SetPostmarkToken(HttpContext context, string token, TimeSpan? lifetime = null)
        => UpdateValue(data => data with { PostmarkToken = Protect(token) });

    public void SetSqlConnectionString(HttpContext context, string connectionString, TimeSpan? lifetime = null)
        => UpdateValue(data => data with { SqlConnectionString = Protect(connectionString) });

    public void SetMetricsEndpoints(
        HttpContext context,
        IReadOnlyList<MetricsEndpointRegistration> endpoints,
        TimeSpan? lifetime = null)
    {
        var json = JsonSerializer.Serialize(endpoints, serializerOptions);
        UpdateValue(data => data with { MetricsEndpoints = Protect(json) });
    }

    public void SetMetricsRefreshSeconds(HttpContext context, int refreshSeconds, TimeSpan? lifetime = null)
        => UpdateValue(data => data with { MetricsRefreshSeconds = Protect(refreshSeconds.ToString(CultureInfo.InvariantCulture)) });

    public void SetPinnedMetrics(HttpContext context, IReadOnlyList<string> metricNames, TimeSpan? lifetime = null)
    {
        var json = JsonSerializer.Serialize(metricNames, serializerOptions);
        UpdateValue(data => data with { MetricsPinnedMetrics = Protect(json) });
    }

    public void ClearPostmarkToken(HttpContext context)
        => UpdateValue(data => data with { PostmarkToken = null });

    public void ClearSqlConnectionString(HttpContext context)
        => UpdateValue(data => data with { SqlConnectionString = null });

    public void ClearMetricsEndpoints(HttpContext context)
        => UpdateValue(data => data with { MetricsEndpoints = null });

    public void ClearMetricsRefreshSeconds(HttpContext context)
        => UpdateValue(data => data with { MetricsRefreshSeconds = null });

    public void ClearPinnedMetrics(HttpContext context)
        => UpdateValue(data => data with { MetricsPinnedMetrics = null });

    private bool TryGetValue(Func<SetupData, string?> selector, out string value)
    {
        value = string.Empty;
        var data = ReadData();
        var protectedValue = selector(data);
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return false;
        }

        try
        {
            value = protector.Unprotect(protectedValue);
            return !string.IsNullOrWhiteSpace(value);
        }
        catch
        {
            value = string.Empty;
#pragma warning disable ERP022 // Unobserved exception in a generic exception handler
            return false;
#pragma warning restore ERP022 // Unobserved exception in a generic exception handler
        }
    }

    private bool TryGetMetricsValue(
        Func<SetupData, string?> selector,
        out IReadOnlyList<MetricsEndpointRegistration> endpoints)
    {
        endpoints = Array.Empty<MetricsEndpointRegistration>();
        if (!TryGetValue(selector, out var json))
        {
            return false;
        }

        try
        {
            endpoints = JsonSerializer.Deserialize<List<MetricsEndpointRegistration>>(json, serializerOptions)
                ?? new List<MetricsEndpointRegistration>();
            return endpoints.Count > 0;
        }
        catch
        {
            endpoints = Array.Empty<MetricsEndpointRegistration>();
#pragma warning disable ERP022 // Unobserved exception in a generic exception handler
            return false;
#pragma warning restore ERP022 // Unobserved exception in a generic exception handler
        }
    }

    private bool TryGetIntValue(Func<SetupData, string?> selector, out int value)
    {
        value = 0;
        if (!TryGetValue(selector, out var text))
        {
            return false;
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private bool TryGetStringListValue(Func<SetupData, string?> selector, out IReadOnlyList<string> values)
    {
        values = Array.Empty<string>();
        if (!TryGetValue(selector, out var json))
        {
            return false;
        }

        try
        {
            values = JsonSerializer.Deserialize<List<string>>(json, serializerOptions)
                ?? new List<string>();
            return values.Count > 0;
        }
        catch
        {
            values = Array.Empty<string>();
#pragma warning disable ERP022 // Unobserved exception in a generic exception handler
            return false;
#pragma warning restore ERP022 // Unobserved exception in a generic exception handler
        }
    }

    private void UpdateValue(Func<SetupData, SetupData> update)
    {
        @lock.Wait();
        try
        {
            var data = ReadDataLocked();
            var updated = update(data);
            var json = JsonSerializer.Serialize(updated, serializerOptions);
            File.WriteAllText(storePath, json);
        }
        finally
        {
            @lock.Release();
        }
    }

    private SetupData ReadData()
    {
        @lock.Wait();
        try
        {
            return ReadDataLocked();
        }
        finally
        {
            @lock.Release();
        }
    }

    private SetupData ReadDataLocked()
    {
        if (!File.Exists(storePath))
        {
            return new SetupData();
        }

        var json = File.ReadAllText(storePath);
        return JsonSerializer.Deserialize<SetupData>(json, serializerOptions) ?? new SetupData();
    }

    private string Protect(string value) => protector.Protect(value);

    private sealed record SetupData
    {
        public string? PostmarkToken { get; init; }
        public string? SqlConnectionString { get; init; }
        public string? MetricsEndpoints { get; init; }
        public string? MetricsRefreshSeconds { get; init; }
        public string? MetricsPinnedMetrics { get; init; }
    }
}
