using System.Net;
using System.Text;

namespace ApiLoadTester.Core.Tests;

/// <summary>Minimal hermetic loopback HTTP server for engine smoke tests, built on the in-box
/// HttpListener rather than pulling ASP.NET Core hosting into a class library test project. Bound to
/// 127.0.0.1 only - never touches the network, matching the "no internet access at runtime" goal.</summary>
public sealed class TestHttpServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _acceptLoop;

    public string BaseUrl { get; }
    public Func<HttpListenerRequest, (int StatusCode, string Body)> Handler { get; set; } = _ => (200, "ok");
    public List<HttpListenerRequest> ReceivedRequests { get; } = new();
    private readonly object _lock = new();

    public TestHttpServer()
    {
        var port = GetFreeTcpPort();
        BaseUrl = $"http://127.0.0.1:{port}/";

        _listener = new HttpListener();
        _listener.Prefixes.Add(BaseUrl);
        _listener.Start();

        _acceptLoop = Task.Run(AcceptLoopAsync);
    }

    private async Task AcceptLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync().WaitAsync(_cts.Token);
            }
            catch (Exception) when (_cts.IsCancellationRequested)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            _ = Task.Run(() => HandleAsync(ctx));
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        lock (_lock)
            ReceivedRequests.Add(ctx.Request);

        var (statusCode, body) = Handler(ctx.Request);
        ctx.Response.StatusCode = statusCode;
        var bytes = Encoding.UTF8.GetBytes(body);
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes);
        ctx.Response.OutputStream.Close();
    }

    private static int GetFreeTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _listener.Close();
        _cts.Dispose();
    }
}
