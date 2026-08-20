namespace ApiLoadTester.Core.Models;

public sealed class TestConfiguration
{
    public required string TargetUrl { get; set; }
    public string HttpMethod { get; set; } = "GET";
    public List<HeaderEntry> Headers { get; set; } = new();

    /// <summary>Optional request body template. Supports {{guid}}, {{seq}}, and {{timestamp}} tokens,
    /// substituted per request by BodyTemplateRenderer so concurrent requests can carry unique payloads.</summary>
    public string? BodyTemplate { get; set; }
    public string ContentType { get; set; } = "application/json";

    public CertificateSource Certificate { get; set; } = new();
    public TlsOptions Tls { get; set; } = new();

    /// <summary>Number of concurrent worker tasks issuing requests.</summary>
    public int Concurrency { get; set; } = 10;

    /// <summary>Wall-clock duration to run for. Null means "run until MaxRequestCount is reached".</summary>
    public TimeSpan? Duration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Total request budget across all workers. Null means "run until Duration elapses".
    /// At least one of Duration / MaxRequestCount must be set.</summary>
    public int? MaxRequestCount { get; set; }

    /// <summary>Per-request timeout.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Delay between starting successive workers, so all N workers don't hit the server in the
    /// same instant. This is a small stagger, not a full ramp-up test pattern.</summary>
    public TimeSpan RampInDelay { get; set; } = TimeSpan.Zero;

    /// <summary>Free-text name shown on the cover page of the generated report.</summary>
    public string? ReportTitle { get; set; }

    /// <summary>Optional customer/account name shown on the cover page of the generated report.</summary>
    public string? CustomerName { get; set; }
}
