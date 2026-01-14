using Bravellian.InfraMonitor.Metrics.AspNetCore;
using Bravellian.InfraMonitor.Metrics.Ui;
using Bravellian.InfraMonitor.Metrics.Ui.Sample;
using Bravellian.InfraMonitor.Metrics.Ui.Services.Setup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBravellianMetricsUi();
builder.Services.AddBravellianMetrics(options =>
{
    options.EnablePrometheusExporter = true;
    options.PrometheusEndpointPath = "/metrics";
});
builder.Services.Configure<MetricsUiOptions>(builder.Configuration.GetSection("MetricsUi"));
builder.Services.AddSingleton<IMetricsSetupStore, SampleMetricsSetupStore>();

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

await app.RunAsync().ConfigureAwait(false);
