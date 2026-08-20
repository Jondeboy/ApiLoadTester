using System.Diagnostics;
using ApiLoadTester.Core.Engine;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Tests;

public class LoadTestEngineSmokeTests
{
    [Fact]
    public async Task RunAsync_AgainstHermeticLoopbackServer_CompletesWithAllSuccesses()
    {
        using var server = new TestHttpServer { Handler = _ => (200, "ok") };
        var engine = new LoadTestEngine();

        var config = new TestConfiguration
        {
            TargetUrl = server.BaseUrl,
            Concurrency = 5,
            Duration = TimeSpan.FromSeconds(1.5),
            RequestTimeout = TimeSpan.FromSeconds(5)
        };

        var summary = await engine.RunAsync(config, new Progress<LiveStatsSnapshot>(), CancellationToken.None);

        // A handful of requests in flight exactly at the duration boundary are legitimately cut off
        // and recorded as Cancelled (not a failure of the server or the engine) - assert "almost all
        // succeeded" rather than "every single one," which would be flaky by construction.
        Assert.True(summary.TotalRequests > 0, "Expected at least one request to complete.");
        Assert.True(summary.SuccessRate >= 0.99, $"Expected near-100% success, got {summary.SuccessRate:P1}.");
    }

    [Fact]
    public async Task RunAsync_ServerReturns503_RecordedAsHttpErrorNotSuccess()
    {
        using var server = new TestHttpServer { Handler = _ => (503, "unavailable") };
        var engine = new LoadTestEngine();

        var config = new TestConfiguration
        {
            TargetUrl = server.BaseUrl,
            Concurrency = 2,
            Duration = TimeSpan.FromSeconds(1),
            RequestTimeout = TimeSpan.FromSeconds(5)
        };

        var summary = await engine.RunAsync(config, new Progress<LiveStatsSnapshot>(), CancellationToken.None);

        Assert.True(summary.TotalRequests > 0);
        Assert.Equal(0, summary.SuccessCount);
        Assert.Equal(summary.TotalRequests, summary.ErrorCount);
        Assert.True(summary.StatusCodeCounts.ContainsKey(503));
    }

    [Fact]
    public async Task RunAsync_CustomHeaders_AreSentToServer()
    {
        using var server = new TestHttpServer { Handler = _ => (200, "ok") };
        var engine = new LoadTestEngine();

        var config = new TestConfiguration
        {
            TargetUrl = server.BaseUrl,
            Concurrency = 1,
            MaxRequestCount = 1,
            RequestTimeout = TimeSpan.FromSeconds(5),
            Headers = [new HeaderEntry { Key = "X-Test-Header", Value = "hello-world" }]
        };

        await engine.RunAsync(config, new Progress<LiveStatsSnapshot>(), CancellationToken.None);

        Assert.Single(server.ReceivedRequests);
        Assert.Equal("hello-world", server.ReceivedRequests[0].Headers["X-Test-Header"]);
    }

    [Fact]
    public async Task RunAsync_BodyTemplateWithSeqToken_ProducesDistinctBodiesPerRequest()
    {
        var receivedBodies = new List<string>();
        var syncRoot = new object();

        using var server = new TestHttpServer
        {
            Handler = req =>
            {
                using var reader = new StreamReader(req.InputStream);
                var body = reader.ReadToEnd();
                lock (syncRoot) receivedBodies.Add(body);
                return (200, "ok");
            }
        };

        var engine = new LoadTestEngine();
        var config = new TestConfiguration
        {
            TargetUrl = server.BaseUrl,
            HttpMethod = "POST",
            Concurrency = 1,
            MaxRequestCount = 5,
            RequestTimeout = TimeSpan.FromSeconds(5),
            BodyTemplate = "{\"seq\":{{seq}}}"
        };

        await engine.RunAsync(config, new Progress<LiveStatsSnapshot>(), CancellationToken.None);

        Assert.Equal(5, receivedBodies.Count);
        Assert.Equal(5, receivedBodies.Distinct().Count());
    }

    [Fact]
    public async Task RunAsync_MaxRequestCountBudget_StopsAtBudgetEvenWithLongDuration()
    {
        using var server = new TestHttpServer { Handler = _ => (200, "ok") };
        var engine = new LoadTestEngine();

        var config = new TestConfiguration
        {
            TargetUrl = server.BaseUrl,
            Concurrency = 3,
            MaxRequestCount = 10,
            Duration = TimeSpan.FromSeconds(30),
            RequestTimeout = TimeSpan.FromSeconds(5)
        };

        var summary = await engine.RunAsync(config, new Progress<LiveStatsSnapshot>(), CancellationToken.None);

        Assert.Equal(10, summary.TotalRequests);
    }

    [Fact]
    public async Task RunAsync_CancelledEarly_StopsPromptlyInsteadOfRunningFullDuration()
    {
        using var server = new TestHttpServer { Handler = _ => (200, "ok") };
        var engine = new LoadTestEngine();

        var config = new TestConfiguration
        {
            TargetUrl = server.BaseUrl,
            Concurrency = 3,
            Duration = TimeSpan.FromSeconds(30),
            RequestTimeout = TimeSpan.FromSeconds(5)
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        var sw = Stopwatch.StartNew();
        var summary = await engine.RunAsync(config, new Progress<LiveStatsSnapshot>(), cts.Token);
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"Expected prompt cancellation, took {sw.Elapsed}.");
        Assert.True(summary.TotalRequests > 0);
    }
}
