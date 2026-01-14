using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace Bravellian.InfraMonitor.Services.Setup;

public sealed class CookieSetupStore : ISetupStore
{
    private const string PostmarkCookieName = "Bravellian.Postmark.Token";
    private const string SqlCookieName = "Bravellian.SqlServer.Connection";
    private const string MetricsCookieName = "Bravellian.Metrics.Endpoints";
    private const string MetricsRefreshCookieName = "Bravellian.Metrics.RefreshSeconds";
    private const string MetricsPinnedCookieName = "Bravellian.Metrics.Pinned";
    private readonly IDataProtector protector;
    private readonly JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web);

    public CookieSetupStore(IDataProtectionProvider provider)
    {
        protector = provider.CreateProtector("Bravellian.InfraMonitor.Setup.CookieStore");
    }

    public SetupStorageMode Mode => SetupStorageMode.Cookie;

    public bool TryGetPostmarkToken(HttpContext context, out string token)
        => TryGetProtected(context, PostmarkCookieName, out token);

    public bool TryGetSqlConnectionString(HttpContext context, out string connectionString)
        => TryGetProtected(context, SqlCookieName, out connectionString);

    public bool TryGetMetricsEndpoints(HttpContext context, out IReadOnlyList<MetricsEndpointRegistration> endpoints)
        => TryGetProtectedList(context, MetricsCookieName, out endpoints);

    public bool TryGetMetricsRefreshSeconds(HttpContext context, out int refreshSeconds)
        => TryGetProtectedInt(context, MetricsRefreshCookieName, out refreshSeconds);

    public bool TryGetPinnedMetrics(HttpContext context, out IReadOnlyList<string> metricNames)
        => TryGetProtectedStringList(context, MetricsPinnedCookieName, out metricNames);

    public bool TryGetPostmarkTokenForServer(out string token)
    {
        token = string.Empty;
        return false;
    }

    public bool TryGetSqlConnectionStringForServer(out string connectionString)
    {
        connectionString = string.Empty;
        return false;
    }

    public bool TryGetMetricsEndpointsForServer(out IReadOnlyList<MetricsEndpointRegistration> endpoints)
    {
        endpoints = Array.Empty<MetricsEndpointRegistration>();
        return false;
    }

    public bool TryGetMetricsRefreshSecondsForServer(out int refreshSeconds)
    {
        refreshSeconds = 0;
        return false;
    }

    public bool TryGetPinnedMetricsForServer(out IReadOnlyList<string> metricNames)
    {
        metricNames = Array.Empty<string>();
        return false;
    }

    public void SetPostmarkToken(HttpContext context, string token, TimeSpan? lifetime = null)
        => SetProtected(context, PostmarkCookieName, token, lifetime);

    public void SetSqlConnectionString(HttpContext context, string connectionString, TimeSpan? lifetime = null)
        => SetProtected(context, SqlCookieName, connectionString, lifetime);

    public void SetMetricsEndpoints(
        HttpContext context,
        IReadOnlyList<MetricsEndpointRegistration> endpoints,
        TimeSpan? lifetime = null)
        => SetProtectedList(context, MetricsCookieName, endpoints, lifetime);

    public void SetMetricsRefreshSeconds(HttpContext context, int refreshSeconds, TimeSpan? lifetime = null)
        => SetProtected(context, MetricsRefreshCookieName, refreshSeconds.ToString(CultureInfo.InvariantCulture), lifetime);

    public void SetPinnedMetrics(HttpContext context, IReadOnlyList<string> metricNames, TimeSpan? lifetime = null)
        => SetProtectedStringList(context, MetricsPinnedCookieName, metricNames, lifetime);

    public void ClearPostmarkToken(HttpContext context)
        => context.Response.Cookies.Delete(PostmarkCookieName);

    public void ClearSqlConnectionString(HttpContext context)
        => context.Response.Cookies.Delete(SqlCookieName);

    public void ClearMetricsEndpoints(HttpContext context)
        => context.Response.Cookies.Delete(MetricsCookieName);

    public void ClearMetricsRefreshSeconds(HttpContext context)
        => context.Response.Cookies.Delete(MetricsRefreshCookieName);

    public void ClearPinnedMetrics(HttpContext context)
        => context.Response.Cookies.Delete(MetricsPinnedCookieName);

    private bool TryGetProtected(HttpContext context, string cookieName, out string value)
    {
        value = string.Empty;
        if (!context.Request.Cookies.TryGetValue(cookieName, out var protectedValue))
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

    private bool TryGetProtectedList(
        HttpContext context,
        string cookieName,
        out IReadOnlyList<MetricsEndpointRegistration> endpoints)
    {
        endpoints = Array.Empty<MetricsEndpointRegistration>();
        if (!TryGetProtected(context, cookieName, out var json))
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

    private bool TryGetProtectedStringList(
        HttpContext context,
        string cookieName,
        out IReadOnlyList<string> values)
    {
        values = Array.Empty<string>();
        if (!TryGetProtected(context, cookieName, out var json))
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

    private bool TryGetProtectedInt(HttpContext context, string cookieName, out int value)
    {
        value = 0;
        if (!TryGetProtected(context, cookieName, out var text))
        {
            return false;
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private void SetProtected(HttpContext context, string cookieName, string value, TimeSpan? lifetime)
    {
        var protectedValue = protector.Protect(value);
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.Add(lifetime ?? TimeSpan.FromDays(30))
        };

        context.Response.Cookies.Append(cookieName, protectedValue, options);
    }

    private void SetProtectedList(
        HttpContext context,
        string cookieName,
        IReadOnlyList<MetricsEndpointRegistration> endpoints,
        TimeSpan? lifetime)
    {
        var json = JsonSerializer.Serialize(endpoints, serializerOptions);
        SetProtected(context, cookieName, json, lifetime);
    }

    private void SetProtectedStringList(
        HttpContext context,
        string cookieName,
        IReadOnlyList<string> values,
        TimeSpan? lifetime)
    {
        var json = JsonSerializer.Serialize(values, serializerOptions);
        SetProtected(context, cookieName, json, lifetime);
    }
}
