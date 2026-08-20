using ApiLoadTester.Core.Engine;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Reporting;

public sealed record TimeBucket(int SecondOffset, long RequestCount, long ErrorCount, double AvgLatencyMs, double P95LatencyMs);

/// <summary>Buckets raw per-request results into 1-second windows so the report can chart
/// throughput and latency over the course of the run.</summary>
public static class TimeSeriesBuilder
{
    public static IReadOnlyList<TimeBucket> BuildPerSecondBuckets(TestSummary summary)
    {
        if (summary.RawResults.Count == 0)
            return [];

        var totalSeconds = Math.Max(1, (int)Math.Ceiling(summary.WallClockDuration.TotalSeconds));
        var buckets = new List<double>[totalSeconds];
        var errorCounts = new long[totalSeconds];
        for (var i = 0; i < totalSeconds; i++)
            buckets[i] = [];

        foreach (var r in summary.RawResults)
        {
            var offset = (int)(r.Timestamp - summary.StartedAt).TotalSeconds;
            offset = Math.Clamp(offset, 0, totalSeconds - 1);
            buckets[offset].Add(r.LatencyMs);
            if (!r.IsSuccess)
                errorCounts[offset]++;
        }

        var result = new List<TimeBucket>(totalSeconds);
        for (var i = 0; i < totalSeconds; i++)
        {
            var latencies = buckets[i];
            var stats = PercentileCalculator.Compute(latencies);
            result.Add(new TimeBucket(i, latencies.Count, errorCounts[i], stats.AvgMs, stats.P95Ms));
        }

        return result;
    }
}
