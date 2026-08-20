using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Threading;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Engine;

public sealed class HttpRequestExecutor : IHttpRequestExecutor
{
    public async Task<RequestResult> ExecuteAsync(TestConfiguration config, HttpClient client, long sequenceNumber, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(config.RequestTimeout);

        var sw = Stopwatch.StartNew();
        var timestamp = DateTimeOffset.UtcNow;

        try
        {
            using var request = BuildRequest(config, sequenceNumber);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token)
                .ConfigureAwait(false);

            var bytes = await response.Content.ReadAsByteArrayAsync(timeoutCts.Token).ConfigureAwait(false);
            sw.Stop();

            var statusCode = (int)response.StatusCode;
            var isSuccess = response.IsSuccessStatusCode;

            return new RequestResult
            {
                SequenceNumber = sequenceNumber,
                Timestamp = timestamp,
                LatencyMs = sw.Elapsed.TotalMilliseconds,
                StatusCode = statusCode,
                Status = isSuccess ? ResultStatus.Success : ResultStatus.HttpError,
                ErrorMessage = isSuccess ? null : $"HTTP {statusCode} {response.ReasonPhrase}",
                ResponseBytes = bytes.LongLength
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            sw.Stop();
            return Failure(sequenceNumber, timestamp, sw, ResultStatus.Cancelled, nameof(OperationCanceledException), "Test was stopped.");
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return Failure(sequenceNumber, timestamp, sw, ResultStatus.Timeout, nameof(TimeoutException), $"Request exceeded the {config.RequestTimeout.TotalSeconds:0.#}s timeout.");
        }
        catch (AuthenticationException ex)
        {
            sw.Stop();
            return Failure(sequenceNumber, timestamp, sw, ResultStatus.TlsError, ex.GetType().Name, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            var status = IsLikelyTlsFailure(ex) ? ResultStatus.TlsError : ResultStatus.ConnectionError;
            return Failure(sequenceNumber, timestamp, sw, status, ex.GetType().Name, ex.Message);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Failure(sequenceNumber, timestamp, sw, ResultStatus.OtherException, ex.GetType().Name, ex.Message);
        }
    }

    private static bool IsLikelyTlsFailure(HttpRequestException ex) =>
        ex.InnerException is AuthenticationException || ex.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("TLS", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("certificate", StringComparison.OrdinalIgnoreCase);

    private static RequestResult Failure(long sequenceNumber, DateTimeOffset timestamp, Stopwatch sw, ResultStatus status, string errorType, string message) =>
        new()
        {
            SequenceNumber = sequenceNumber,
            Timestamp = timestamp,
            LatencyMs = sw.Elapsed.TotalMilliseconds,
            StatusCode = null,
            Status = status,
            ErrorType = errorType,
            ErrorMessage = message,
            ResponseBytes = 0
        };

    private static HttpRequestMessage BuildRequest(TestConfiguration config, long sequenceNumber)
    {
        var request = new HttpRequestMessage(new HttpMethod(config.HttpMethod), config.TargetUrl);

        foreach (var header in config.Headers)
        {
            if (string.IsNullOrWhiteSpace(header.Key))
                continue;
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var renderedBody = BodyTemplateRenderer.Render(config.BodyTemplate, sequenceNumber);
        if (!string.IsNullOrEmpty(renderedBody) && HttpMethodAllowsBody(config.HttpMethod))
        {
            request.Content = new StringContent(renderedBody);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(config.ContentType);
        }

        return request;
    }

    private static bool HttpMethodAllowsBody(string method) =>
        !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase);
}
