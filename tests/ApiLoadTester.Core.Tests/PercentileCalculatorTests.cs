using ApiLoadTester.Core.Engine;

namespace ApiLoadTester.Core.Tests;

public class PercentileCalculatorTests
{
    [Fact]
    public void Compute_EmptyCollection_ReturnsEmptyStats()
    {
        var result = PercentileCalculator.Compute(Array.Empty<double>());

        Assert.Equal(0, result.MinMs);
        Assert.Equal(0, result.MaxMs);
        Assert.Equal(0, result.AvgMs);
    }

    [Fact]
    public void Compute_SingleValue_AllStatsEqualThatValue()
    {
        var result = PercentileCalculator.Compute([42.0]);

        Assert.Equal(42, result.MinMs);
        Assert.Equal(42, result.MaxMs);
        Assert.Equal(42, result.AvgMs);
        Assert.Equal(42, result.MedianMs);
        Assert.Equal(42, result.P99Ms);
    }

    [Fact]
    public void Compute_OneToHundred_MatchesKnownNearestRankPercentiles()
    {
        var values = Enumerable.Range(1, 100).Select(i => (double)i).ToArray();

        var result = PercentileCalculator.Compute(values);

        Assert.Equal(1, result.MinMs);
        Assert.Equal(100, result.MaxMs);
        Assert.Equal(50.5, result.AvgMs);
        Assert.Equal(50, result.MedianMs);
        Assert.Equal(90, result.P90Ms);
        Assert.Equal(95, result.P95Ms);
        Assert.Equal(99, result.P99Ms);
    }

    [Fact]
    public void Percentile_IsOrderIndependent()
    {
        double[] ascending = [10, 20, 30, 40, 50];
        double[] shuffled = [50, 10, 40, 20, 30];

        var a = PercentileCalculator.Compute(ascending);
        var b = PercentileCalculator.Compute(shuffled);

        Assert.Equal(a.P90Ms, b.P90Ms);
        Assert.Equal(a.MedianMs, b.MedianMs);
    }
}
