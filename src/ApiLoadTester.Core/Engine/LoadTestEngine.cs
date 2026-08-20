using System.Net.Security;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using ApiLoadTester.Core.Certificates;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Engine;

/// <summary>Constant-load engine: N concurrent workers issue requests, staggered by an optional
/// ramp-in delay, until the configured duration elapses and/or the request budget is exhausted.</summary>
public sealed class LoadTestEngine : ILoadTestEngine
{
    private static readonly TimeSpan LiveSnapshotInterval = TimeSpan.FromMilliseconds(250);

    private readonly IHttpRequestExecutor _executor;
    private readonly CertificateProviderFactory _certificateProviderFactory;

    public LoadTestEngine(IHttpRequestExecutor? executor = null, CertificateProviderFactory? certificateProviderFactory = null)
    {
        _executor = executor ?? new HttpRequestExecutor();
        _certificateProviderFactory = certificateProviderFactory ?? new CertificateProviderFactory();
    }

    public async Task<TestSummary> RunAsync(TestConfiguration config, IProgress<LiveStatsSnapshot> progress, CancellationToken ct)
    {
        if (config.Duration is null && config.MaxRequestCount is null)
            throw new ArgumentException("At least one of Duration or MaxRequestCount must be set.", nameof(config));
        if (config.Concurrency < 1)
            throw new ArgumentException("Concurrency must be at least 1.", nameof(config));

        var startedAt = DateTimeOffset.UtcNow;
        var aggregator = new MetricsAggregator();

        X509Certificate2? clientCert = _certificateProviderFactory.Resolve(config.Certificate);
        using var handler = BuildHandler(config, clientCert);
        using var client = new HttpClient(handler, disposeHandler: false)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (config.Duration is { } duration)
            runCts.CancelAfter(duration);

        long sequenceCounter = 0;
        var budget = config.MaxRequestCount;

        var progressTask = ReportProgressPeriodically(aggregator, progress, runCts.Token);

        var workers = Enumerable.Range(0, config.Concurrency).Select(async workerIndex =>
        {
            if (config.RampInDelay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(config.RampInDelay.TotalMilliseconds * workerIndex), runCts.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            while (!runCts.IsCancellationRequested)
            {
                if (budget is not null)
                {
                    var next = Interlocked.Increment(ref sequenceCounter);
                    if (next > budget.Value)
                        break;
                }
                else
                {
                    Interlocked.Increment(ref sequenceCounter);
                }

                var result = await _executor.ExecuteAsync(config, client, sequenceCounter, runCts.Token).ConfigureAwait(false);
                aggregator.Add(result);

                if (result.Status == ResultStatus.Cancelled)
                    break;
            }
        }).ToArray();

        await Task.WhenAll(workers).ConfigureAwait(false);
        await runCts.CancelAsync().ConfigureAwait(false);
        await progressTask.ConfigureAwait(false);

        var finishedAt = DateTimeOffset.UtcNow;
        progress.Report(aggregator.GetLiveSnapshot());

        clientCert?.Dispose();

        return aggregator.BuildSummary(config, startedAt, finishedAt);
    }

    private static async Task ReportProgressPeriodically(MetricsAggregator aggregator, IProgress<LiveStatsSnapshot> progress, CancellationToken ct)
    {
        try
        {
            using var timer = new PeriodicTimer(LiveSnapshotInterval);
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                progress.Report(aggregator.GetLiveSnapshot());
            }
        }
        catch (OperationCanceledException)
        {
            // Run finished or was stopped - the final snapshot is reported explicitly by the caller.
        }
    }

    private SocketsHttpHandler BuildHandler(TestConfiguration config, X509Certificate2? clientCert)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = Math.Max(config.Concurrency, 1) * 2,
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = config.Tls.MinimumProtocol
            }
        };

        if (clientCert is not null)
        {
            handler.SslOptions.ClientCertificates = new X509CertificateCollection { clientCert };
        }

        if (!config.Tls.ValidateServerCertificate)
        {
            handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        }
        else if (!string.IsNullOrWhiteSpace(config.Tls.ExtraTrustedCaPath))
        {
            var extraCa = X509CertificateLoader.LoadCertificateFromFile(config.Tls.ExtraTrustedCaPath);
            handler.SslOptions.RemoteCertificateValidationCallback = (_, certificate, chain, sslPolicyErrors) =>
            {
                if (sslPolicyErrors == SslPolicyErrors.None)
                    return true;
                if (certificate is null || chain is null)
                    return false;

                chain.ChainPolicy.ExtraStore.Add(extraCa);
                chain.ChainPolicy.VerificationFlags |= X509VerificationFlags.AllowUnknownCertificateAuthority;
                return chain.Build(new X509Certificate2(certificate));
            };
        }

        return handler;
    }
}
