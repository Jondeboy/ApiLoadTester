using System.Security;

namespace ApiLoadTester.Core.Models;

public enum CertificateSourceKind
{
    None,
    PfxFile,
    WindowsStore
}

/// <summary>
/// Describes where to load the mTLS client certificate from. The password (when present) is held
/// only as a <see cref="SecureString"/> for the lifetime of a run and is never serialized to disk
/// in plain text - see PasswordProtector for the opt-in encrypted-at-rest path.
/// </summary>
public sealed class CertificateSource
{
    public CertificateSourceKind Kind { get; set; } = CertificateSourceKind.None;

    /// <summary>Absolute path to a .pfx/.p12 file. Used when Kind == PfxFile.</summary>
    public string? PfxFilePath { get; set; }

    /// <summary>Password for the .pfx file. Never persisted in plain text.</summary>
    public SecureString? Password { get; set; }

    /// <summary>Certificate thumbprint (preferred) or subject name to locate in CurrentUser\My. Used when Kind == WindowsStore.</summary>
    public string? StoreThumbprintOrSubject { get; set; }
}
