using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Engine;

/// <summary>Computes latency statistics using the nearest-rank percentile method for determinism.</summary>
public static class PercentileCalculator
{
    public static LatencyStats Compute(IReadOnlyCollection<double> latenciesMs)
    {
        if (latenciesMs.Count == 0)
            return LatencyStats.Empty;

        var sorted = latenciesMs.ToArray();
        Array.Sort(sorted);

        return new LatencyStats(
            MinMs: sorted[0],
            AvgMs: sorted.Average(),
            MedianMs: Percentile(sorted, 0.50),
            P90Ms: Percentile(sorted, 0.90),
            P95Ms: Percentile(sorted, 0.95),
            P99Ms: Percentile(sorted, 0.99),
            MaxMs: sorted[^1]);
    }

    /// <summary>Nearest-rank percentile over an already-sorted (ascending) array.</summary>
    public static double Percentile(double[] sortedAscending, double p)
    {
        if (sortedAscending.Length == 0)
            return 0;
        if (sortedAscending.Length == 1)
            return sortedAscending[0];

        var rank = (int)Math.Ceiling(p * sortedAscending.Length) - 1;
        var index = Math.Clamp(rank, 0, sortedAscending.Length - 1);
        return sortedAscending[index];
    }
}
