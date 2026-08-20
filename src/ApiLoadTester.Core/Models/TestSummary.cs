namespace ApiLoadTester.Core.Models;

/// <summary>Final result of a completed (or stopped) load test run, plus the raw per-request data
/// needed to render charts and the CSV export.</summary>
public sealed class TestSummary
{
    public required TestConfiguration Configuration { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset FinishedAt { get; init; }
    public TimeSpan WallClockDuration => FinishedAt - StartedAt;

    public long TotalRequests { get; init; }
    public long SuccessCount { get; init; }
    public long ErrorCount { get; init; }
    public double OverallRequestsPerSecond { get; init; }
    public long TotalResponseBytes { get; init; }

    public required LatencyStats Latency { get; init; }
    public required IReadOnlyDictionary<int, long> StatusCodeCounts { get; init; }
    public required IReadOnlyDictionary<string, long> ErrorTypeCounts { get; init; }

    public required IReadOnlyList<RequestResult> RawResults { get; init; }

    public double SuccessRate => TotalRequests == 0 ? 0 : (double)SuccessCount / TotalRequests;
}
