namespace ApiLoadTester.Core.Models;

public sealed record LatencyStats(
    double MinMs,
    double AvgMs,
    double MedianMs,
    double P90Ms,
    double P95Ms,
    double P99Ms,
    double MaxMs)
{
    public static readonly LatencyStats Empty = new(0, 0, 0, 0, 0, 0, 0);
}
