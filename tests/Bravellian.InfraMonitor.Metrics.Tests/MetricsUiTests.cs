using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bravellian.InfraMonitor.Metrics.Ui;
using Bravellian.InfraMonitor.Metrics.Ui.Models;
using Bravellian.InfraMonitor.Metrics.Ui.Services.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bravellian.InfraMonitor.Metrics.Tests;

[TestClass]
public sealed class MetricsUiTests
{
    [TestMethod]
    public void MetricsHistoryStore_EnforcesMaxPoints()
    {
        var store = new MetricsHistoryStore(maxPoints: 2);
        var values = new Dictionary<string, double>(StringComparer.Ordinal) { ["a"] = 1 };

        store.AddSnapshot("key", DateTimeOffset.UtcNow.AddSeconds(-2), values);
        store.AddSnapshot("key", DateTimeOffset.UtcNow.AddSeconds(-1), values);
        store.AddSnapshot("key", DateTimeOffset.UtcNow, values);

        var history = store.GetHistory("key");
        Assert.HasCount(2, history);
    }

    [TestMethod]
    public void PrometheusMetricsParser_ParsesMetadataAndSamples()
    {
        var payload = string.Join('\n',
            "# HELP my_counter Total items",
            "# TYPE my_counter counter",
            "my_counter{status=\"ok\"} 5 123");

        var snapshot = PrometheusMetricsParser.Parse(payload);

        Assert.HasCount(1, snapshot.Samples);
        Assert.AreEqual("my_counter", snapshot.Samples[0].Name);
        Assert.AreEqual("5", snapshot.Samples[0].Value);
        Assert.AreEqual(123, snapshot.Samples[0].Timestamp);
        Assert.IsTrue(snapshot.Metadata.ContainsKey("my_counter"));
        Assert.AreEqual("counter", snapshot.Metadata["my_counter"].Type);
        Assert.AreEqual("Total items", snapshot.Metadata["my_counter"].Help);
    }

    [TestMethod]
    public void PrometheusMetricsParser_HandlesEscapedLabels()
    {
        var payload = "demo_metric{path=\"/api/v1\",note=\"line\\nfeed\",quote=\"\\\"\"} 1";

        var snapshot = PrometheusMetricsParser.Parse(payload);

        Assert.HasCount(1, snapshot.Samples);
        var sample = snapshot.Samples[0];
        Assert.AreEqual("/api/v1", sample.Labels["path"]);
        Assert.AreEqual("line\nfeed", sample.Labels["note"]);
        Assert.AreEqual("\"", sample.Labels["quote"]);
    }

    [TestMethod]
    public async Task MetricsScrapeService_ReturnsSnapshotOnSuccessAsync()
    {
        var payload = "sample_metric 10";
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload)
            });
        using var httpClient = new HttpClient(handler);
        var service = new MetricsScrapeService(httpClient);
        var registration = new MetricsEndpointRegistration
        {
            ServiceName = "svc",
            InstanceName = "inst",
            EndpointUrl = "http://localhost/metrics"
        };

        var report = await service.ScrapeAsync(registration, CancellationToken.None);

        Assert.IsNull(report.Error);
        Assert.IsNotNull(report.Snapshot);
        Assert.AreEqual(registration, report.Registration);
    }

    [TestMethod]
    public async Task MetricsScrapeService_ReturnsErrorOnFailureAsync()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var httpClient = new HttpClient(handler);
        var service = new MetricsScrapeService(httpClient);
        var registration = new MetricsEndpointRegistration
        {
            ServiceName = "svc",
            InstanceName = "inst",
            EndpointUrl = "http://localhost/metrics"
        };

        var report = await service.ScrapeAsync(registration, CancellationToken.None);

        Assert.IsNotNull(report.Error);
        Assert.IsNull(report.Snapshot);
    }

    [TestMethod]
    public void AddBravellianMetricsUi_RegistersUiServices()
    {
        var services = new ServiceCollection();

        services.AddBravellianMetricsUi();

        Assert.IsTrue(services.Any(service => service.ServiceType == typeof(MetricsHistoryStore)));
        Assert.IsTrue(services.Any(service => service.ServiceType == typeof(InfraMonitorAppMetrics)));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            this.handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
