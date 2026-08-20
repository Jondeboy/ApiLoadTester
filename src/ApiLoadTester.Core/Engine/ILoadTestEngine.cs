using System.Threading;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Engine;

public interface ILoadTestEngine
{
    Task<TestSummary> RunAsync(TestConfiguration config, IProgress<LiveStatsSnapshot> progress, CancellationToken ct);
}
