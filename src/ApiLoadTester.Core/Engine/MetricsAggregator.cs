using System.Diagnostics;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Engine;

/// <summary>Thread-safe accumulator for RequestResults produced by concurrent workers. Supports both
/// periodic "live" snapshots (windowed throughput + full-run percentiles so far) and a final summary.</summary>
public sealed class MetricsAggregator
{
    private readonly object _lock = new();
    private readonly List<RequestResult> _results = new();
    private readonly Dictionary<int, long> _statusCodeCounts = new();
    private readonly Dictionary<string, long> _errorTypeCounts = new();

    private long _successCount;
    private long _errorCount;
    private long _totalBytes;

    private readonly Stopwatch _runClock = Stopwatch.StartNew();
    private long _requestsAtLastSnapshot;
    private TimeSpan _elapsedAtLastSnapshot = TimeSpan.Zero;

    public void Add(RequestResult result)
    {
        lock (_lock)
        {
            _results.Add(result);

            if (result.IsSuccess)
                _successCount++;
            else
                _errorCount++;

            _totalBytes += result.ResponseBytes;

            if (result.StatusCode is { } code)
                _statusCodeCounts[code] = _statusCodeCounts.GetValueOrDefault(code) + 1;

            if (result.ErrorType is { } errorType)
                _errorTypeCounts[errorType] = _errorTypeCounts.GetValueOrDefault(errorType) + 1;
        }
    }

    public LiveStatsSnapshot GetLiveSnapshot()
    {
        lock (_lock)
        {
            var elapsed = _runClock.Elapsed;
            var total = _results.Count;

            var windowElapsed = elapsed - _elapsedAtLastSnapshot;
            var windowCount = total - _requestsAtLastSnapshot;
            var currentRps = windowElapsed.TotalSeconds > 0 ? windowCount / windowElapsed.TotalSeconds : 0;
            var overallRps = elapsed.TotalSeconds > 0 ? total / elapsed.TotalSeconds : 0;

            _requestsAtLastSnapshot = total;
            _elapsedAtLastSnapshot = elapsed;

            return new LiveStatsSnapshot(
                Elapsed: elapsed,
                TotalRequests: total,
                SuccessCount: _successCount,
                ErrorCount: _errorCount,
                CurrentRequestsPerSecond: currentRps,
                OverallRequestsPerSecond: overallRps,
                Latency: PercentileCalculator.Compute(_results.Select(r => r.LatencyMs).ToArray()),
                StatusCodeCounts: new Dictionary<int, long>(_statusCodeCounts));
        }
    }

    public TestSummary BuildSummary(TestConfiguration config, DateTimeOffset startedAt, DateTimeOffset finishedAt)
    {
        lock (_lock)
        {
            var wallClockSeconds = (finishedAt - startedAt).TotalSeconds;
            var total = _results.Count;

            return new TestSummary
            {
                Configuration = config,
                StartedAt = startedAt,
                FinishedAt = finishedAt,
                TotalRequests = total,
                SuccessCount = _successCount,
                ErrorCount = _errorCount,
                OverallRequestsPerSecond = wallClockSeconds > 0 ? total / wallClockSeconds : 0,
                TotalResponseBytes = _totalBytes,
                Latency = PercentileCalculator.Compute(_results.Select(r => r.LatencyMs).ToArray()),
                StatusCodeCounts = new Dictionary<int, long>(_statusCodeCounts),
                ErrorTypeCounts = new Dictionary<string, long>(_errorTypeCounts),
                RawResults = _results.ToList()
            };
        }
    }
}
