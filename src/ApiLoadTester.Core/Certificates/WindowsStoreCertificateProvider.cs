using System.Security.Cryptography.X509Certificates;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Certificates;

/// <summary>
/// Resolves a client certificate already installed in the current Windows user's certificate store
/// (CurrentUser\My). This is the recommended alternative for enterprises whose policy forbids
/// exporting private keys to a .pfx file on disk - the private key never leaves the store.
/// </summary>
public sealed class WindowsStoreCertificateProvider : ICertificateProvider
{
    public bool CanResolve(CertificateSourceKind kind) => kind == CertificateSourceKind.WindowsStore;

    public X509Certificate2 Resolve(CertificateSource source)
    {
        if (string.IsNullOrWhiteSpace(source.StoreThumbprintOrSubject))
            throw new ArgumentException("A certificate thumbprint or subject name is required.", nameof(source));

        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);

        var key = source.StoreThumbprintOrSubject.Trim();
        var byThumbprint = store.Certificates.Find(X509FindType.FindByThumbprint, key, validOnly: false);
        var match = byThumbprint.Count > 0
            ? byThumbprint
            : store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, key, validOnly: false);

        if (match.Count == 0)
            match = store.Certificates.Find(X509FindType.FindBySubjectName, key, validOnly: false);

        if (match.Count == 0)
            throw new InvalidOperationException(
                $"No certificate matching thumbprint/subject '{key}' was found in CurrentUser\\My.");

        // Return a fresh instance detached from the store's collection lifetime.
        return new X509Certificate2(match[0]);
    }
}
