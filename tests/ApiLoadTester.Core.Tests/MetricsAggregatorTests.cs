using ApiLoadTester.Core.Engine;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Tests;

public class MetricsAggregatorTests
{
    private static RequestResult Success(long seq, double latencyMs, int statusCode = 200) => new()
    {
        SequenceNumber = seq,
        Timestamp = DateTimeOffset.UtcNow,
        LatencyMs = latencyMs,
        StatusCode = statusCode,
        Status = ResultStatus.Success,
        ResponseBytes = 100
    };

    private static RequestResult Failure(long seq, double latencyMs, int statusCode, string errorType) => new()
    {
        SequenceNumber = seq,
        Timestamp = DateTimeOffset.UtcNow,
        LatencyMs = latencyMs,
        StatusCode = statusCode,
        Status = ResultStatus.HttpError,
        ErrorType = errorType,
        ErrorMessage = $"HTTP {statusCode}",
        ResponseBytes = 0
    };

    [Fact]
    public void BuildSummary_MixedResults_ComputesCorrectCounts()
    {
        var aggregator = new MetricsAggregator();
        aggregator.Add(Success(1, 10));
        aggregator.Add(Success(2, 20));
        aggregator.Add(Failure(3, 30, 503, "HttpError"));

        var summary = aggregator.BuildSummary(
            new TestConfiguration { TargetUrl = "https://example.com" },
            DateTimeOffset.UtcNow.AddSeconds(-1),
            DateTimeOffset.UtcNow);

        Assert.Equal(3, summary.TotalRequests);
        Assert.Equal(2, summary.SuccessCount);
        Assert.Equal(1, summary.ErrorCount);
        Assert.Equal(200, summary.TotalResponseBytes);
        Assert.Equal(1, summary.StatusCodeCounts[503]);
        Assert.Equal(1, summary.ErrorTypeCounts["HttpError"]);
        Assert.Equal(3, summary.RawResults.Count);
    }

    [Fact]
    public void BuildSummary_NoResults_DoesNotThrowAndReportsZero()
    {
        var aggregator = new MetricsAggregator();

        var summary = aggregator.BuildSummary(
            new TestConfiguration { TargetUrl = "https://example.com" },
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        Assert.Equal(0, summary.TotalRequests);
        Assert.Equal(0, summary.SuccessRate);
    }

    [Fact]
    public void GetLiveSnapshot_ReflectsAddedResultsSoFar()
    {
        var aggregator = new MetricsAggregator();
        aggregator.Add(Success(1, 15));
        aggregator.Add(Success(2, 25));

        var snapshot = aggregator.GetLiveSnapshot();

        Assert.Equal(2, snapshot.TotalRequests);
        Assert.Equal(2, snapshot.SuccessCount);
        Assert.Equal(0, snapshot.ErrorCount);
    }
}
