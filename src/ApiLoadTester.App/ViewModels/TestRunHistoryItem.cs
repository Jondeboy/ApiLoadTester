using ApiLoadTester.Core.Models;

namespace ApiLoadTester.App.ViewModels;

public sealed class TestRunHistoryItem
{
    public required TestSummary Summary { get; init; }

    public string Label =>
        $"{Summary.StartedAt.ToLocalTime():HH:mm:ss} — {Summary.Configuration.TargetUrl} " +
        $"({Summary.OverallRequestsPerSecond:0.#} req/s, {Summary.SuccessRate:P0} success)";
}
