namespace ApiLoadTester.Core.Models;

/// <summary>Periodic snapshot pushed from the engine to the UI while a test is running.</summary>
public sealed record LiveStatsSnapshot(
    TimeSpan Elapsed,
    long TotalRequests,
    long SuccessCount,
    long ErrorCount,
    double CurrentRequestsPerSecond,
    double OverallRequestsPerSecond,
    LatencyStats Latency,
    IReadOnlyDictionary<int, long> StatusCodeCounts);
