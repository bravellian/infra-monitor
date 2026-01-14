using System;
using Bravellian.InfraMonitor.Metrics.AspNetCore;
using Bravellian.InfraMonitor.Metrics.Ui;
using Bravellian.InfraMonitor.Metrics.Ui.Services.Setup;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddBravellianMetricsUi();
builder.Services.AddDataProtection();
builder.Services.AddBravellianMetrics(options =>
{
    options.EnablePrometheusExporter = true;
    options.PrometheusEndpointPath = "/metrics";
});
builder.Services.Configure<Bravellian.InfraMonitor.Services.Setup.SetupStorageOptions>(
    builder.Configuration.GetSection("SetupStorage"));
builder.Services.Configure<Bravellian.InfraMonitor.Services.Postmark.PostmarkCacheOptions>(
    builder.Configuration.GetSection("PostmarkCache"));
builder.Services.Configure<Bravellian.InfraMonitor.Services.SqlServer.SqlSnapshotOptions>(
    builder.Configuration.GetSection("SqlSnapshots"));
builder.Services.AddSingleton<Bravellian.InfraMonitor.Services.Postmark.PostmarkCache>();
builder.Services.AddSingleton<Bravellian.InfraMonitor.Services.Postmark.PostmarkAnalyzer>();
builder.Services.AddSingleton<Bravellian.InfraMonitor.Services.SqlServer.SqlServerReporter>();
builder.Services.AddSingleton<Bravellian.InfraMonitor.Services.SqlServer.SqlSnapshotStore>();
builder.Services.AddHostedService<Bravellian.InfraMonitor.Services.SqlServer.SqlSnapshotHostedService>();
builder.Services.AddSingleton<Bravellian.InfraMonitor.Services.Setup.CookieSetupStore>();
builder.Services.AddSingleton<Bravellian.InfraMonitor.Services.Setup.ServerSetupStore>();
builder.Services.AddSingleton<Bravellian.InfraMonitor.Services.Setup.ISetupStore>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Bravellian.InfraMonitor.Services.Setup.SetupStorageOptions>>().Value;
    return options.Mode == Bravellian.InfraMonitor.Services.Setup.SetupStorageMode.Server
        ? sp.GetRequiredService<Bravellian.InfraMonitor.Services.Setup.ServerSetupStore>()
        : sp.GetRequiredService<Bravellian.InfraMonitor.Services.Setup.CookieSetupStore>();
});
builder.Services.AddSingleton<IMetricsSetupStore, Bravellian.InfraMonitor.Services.Setup.MetricsSetupStoreAdapter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapBravellianMetricsEndpoint();

await app.RunAsync().ConfigureAwait(false);
