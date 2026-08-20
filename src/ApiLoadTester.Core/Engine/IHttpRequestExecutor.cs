using System.Threading;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Engine;

public interface IHttpRequestExecutor
{
    Task<RequestResult> ExecuteAsync(TestConfiguration config, HttpClient client, long sequenceNumber, CancellationToken ct);
}
