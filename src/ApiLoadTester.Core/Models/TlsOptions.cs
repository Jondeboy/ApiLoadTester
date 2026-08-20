using System.Security.Authentication;

namespace ApiLoadTester.Core.Models;

public sealed class TlsOptions
{
    public SslProtocols MinimumProtocol { get; set; } = SslProtocols.Tls12;

    /// <summary>
    /// When false, server certificate validation is bypassed entirely. This is only ever useful against
    /// a known internal test environment with a self-signed server cert, never against a customer's
    /// production endpoint. The UI must surface a prominent warning whenever this is off, and the
    /// generated report flags it explicitly in the methodology section.
    /// </summary>
    public bool ValidateServerCertificate { get; set; } = true;

    /// <summary>Optional path to an extra CA certificate (.cer/.crt/.pem) to trust in addition to the
    /// machine trust store - useful for enterprise TLS-inspecting proxies.</summary>
    public string? ExtraTrustedCaPath { get; set; }
}
