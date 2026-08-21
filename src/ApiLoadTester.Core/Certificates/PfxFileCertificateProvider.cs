using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Certificates;

/// <summary>
/// Loads a client certificate from a .pfx/.p12 file on disk. The password is only ever held as a
/// SecureString by the caller; it is marshaled to unmanaged memory right here, at the point of use,
/// and zeroed immediately afterwards so plaintext never lingers in managed memory or gets captured
/// by a debugger heap dump longer than necessary.
///
/// Deliberately does NOT use X509KeyStorageFlags.EphemeralKeySet: on Windows, SChannel (which
/// SslStream/HttpClient use for TLS) cannot present an ephemeral-keyset certificate as a client
/// certificate during the handshake at all - it fails with "Authentication failed because the
/// platform does not support ephemeral keys," even though loading the certificate itself succeeds.
/// DefaultKeySet uses a normal (if short-lived/process-scoped) CNG key container, which is what
/// mutual-TLS client auth actually requires.
/// </summary>
public sealed class PfxFileCertificateProvider : ICertificateProvider
{
    public bool CanResolve(CertificateSourceKind kind) => kind == CertificateSourceKind.PfxFile;

    public X509Certificate2 Resolve(CertificateSource source)
    {
        if (string.IsNullOrWhiteSpace(source.PfxFilePath))
            throw new ArgumentException("A .pfx file path is required.", nameof(source));
        if (!File.Exists(source.PfxFilePath))
            throw new FileNotFoundException("Certificate file not found.", source.PfxFilePath);

        return LoadWithPassword(source.PfxFilePath, source.Password);
    }

    private static X509Certificate2 LoadWithPassword(string path, SecureString? password)
    {
        nint unmanagedPtr = nint.Zero;
        try
        {
            ReadOnlySpan<char> passwordSpan = default;
            int length = 0;
            if (password is { Length: > 0 })
            {
                unmanagedPtr = Marshal.SecureStringToGlobalAllocUnicode(password);
                length = password.Length;
            }

            unsafe
            {
                passwordSpan = unmanagedPtr == nint.Zero
                    ? default
                    : new ReadOnlySpan<char>((void*)unmanagedPtr, length);

                return X509CertificateLoader.LoadPkcs12FromFile(path, passwordSpan, X509KeyStorageFlags.DefaultKeySet);
            }
        }
        finally
        {
            if (unmanagedPtr != nint.Zero)
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedPtr);
        }
    }
}
