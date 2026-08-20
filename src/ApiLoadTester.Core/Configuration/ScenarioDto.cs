using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Configuration;

/// <summary>
/// JSON-serializable mirror of TestConfiguration. Deliberately separate from TestConfiguration
/// because CertificateSource.Password is a SecureString (not serializable, and it shouldn't be) -
/// this DTO instead carries an optional DPAPI-encrypted blob, only ever written when the user
/// explicitly opts in to "remember password".
/// </summary>
public sealed class ScenarioDto
{
    public string SchemaVersion { get; set; } = "1";

    public required string TargetUrl { get; set; }
    public string HttpMethod { get; set; } = "GET";
    public List<HeaderEntry> Headers { get; set; } = new();
    public string? BodyTemplate { get; set; }
    public string ContentType { get; set; } = "application/json";

    public CertificateSourceKind CertificateKind { get; set; } = CertificateSourceKind.None;
    public string? PfxFilePath { get; set; }
    public string? StoreThumbprintOrSubject { get; set; }

    /// <summary>DPAPI-protected (CurrentUser scope) password blob. Null unless the user opted in to
    /// "remember password". Only decrypts on the same Windows account/machine that wrote it.</summary>
    public string? EncryptedPassword { get; set; }

    public string MinimumTlsProtocol { get; set; } = "Tls12";
    public bool ValidateServerCertificate { get; set; } = true;
    public string? ExtraTrustedCaPath { get; set; }

    public int Concurrency { get; set; } = 10;
    public double? DurationSeconds { get; set; }
    public int? MaxRequestCount { get; set; }
    public double RequestTimeoutSeconds { get; set; } = 30;
    public double RampInDelayMs { get; set; }

    public string? ReportTitle { get; set; }
    public string? CustomerName { get; set; }
}
