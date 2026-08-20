using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ApiLoadTester.Core.Certificates;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Tests;

public class CertificateProviderTests
{
    private static X509Certificate2 CreateSelfSignedTestCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=ApiLoadTester Test Cert", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(5));
    }

    private static SecureString ToSecureString(string plain)
    {
        var secure = new SecureString();
        foreach (var c in plain)
            secure.AppendChar(c);
        secure.MakeReadOnly();
        return secure;
    }

    [Fact]
    public void PfxFileCertificateProvider_LoadsCertificateWithMatchingThumbprint()
    {
        using var original = CreateSelfSignedTestCertificate();
        var pfxBytes = original.Export(X509ContentType.Pfx, "test-password-123");

        var tempPath = Path.Combine(Path.GetTempPath(), $"loadtester-test-{Guid.NewGuid():N}.pfx");
        File.WriteAllBytes(tempPath, pfxBytes);

        try
        {
            var provider = new PfxFileCertificateProvider();
            var source = new CertificateSource
            {
                Kind = CertificateSourceKind.PfxFile,
                PfxFilePath = tempPath,
                Password = ToSecureString("test-password-123")
            };

            using var loaded = provider.Resolve(source);

            Assert.Equal(original.Thumbprint, loaded.Thumbprint);
            Assert.True(loaded.HasPrivateKey);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void PfxFileCertificateProvider_WrongPassword_ThrowsCryptographicException()
    {
        using var original = CreateSelfSignedTestCertificate();
        var pfxBytes = original.Export(X509ContentType.Pfx, "correct-password");
        var tempPath = Path.Combine(Path.GetTempPath(), $"loadtester-test-{Guid.NewGuid():N}.pfx");
        File.WriteAllBytes(tempPath, pfxBytes);

        try
        {
            var provider = new PfxFileCertificateProvider();
            var source = new CertificateSource
            {
                Kind = CertificateSourceKind.PfxFile,
                PfxFilePath = tempPath,
                Password = ToSecureString("wrong-password")
            };

            Assert.Throws<CryptographicException>(() => provider.Resolve(source));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void PfxFileCertificateProvider_MissingFile_ThrowsFileNotFound()
    {
        var provider = new PfxFileCertificateProvider();
        var source = new CertificateSource
        {
            Kind = CertificateSourceKind.PfxFile,
            PfxFilePath = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.pfx"),
            Password = ToSecureString("x")
        };

        Assert.Throws<FileNotFoundException>(() => provider.Resolve(source));
    }

    [Fact]
    public void WindowsStoreCertificateProvider_ResolvesByThumbprint()
    {
        using var original = CreateSelfSignedTestCertificate();

        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);
        store.Add(original);

        try
        {
            var provider = new WindowsStoreCertificateProvider();
            var source = new CertificateSource
            {
                Kind = CertificateSourceKind.WindowsStore,
                StoreThumbprintOrSubject = original.Thumbprint
            };

            using var resolved = provider.Resolve(source);

            Assert.Equal(original.Thumbprint, resolved.Thumbprint);
        }
        finally
        {
            store.Remove(original);
        }
    }

    [Fact]
    public void WindowsStoreCertificateProvider_NotFound_Throws()
    {
        var provider = new WindowsStoreCertificateProvider();
        var source = new CertificateSource
        {
            Kind = CertificateSourceKind.WindowsStore,
            StoreThumbprintOrSubject = "0000000000000000000000000000000000AAAA"
        };

        Assert.Throws<InvalidOperationException>(() => provider.Resolve(source));
    }

    [Fact]
    public void CertificateProviderFactory_None_ReturnsNull()
    {
        var factory = new CertificateProviderFactory();
        var result = factory.Resolve(new CertificateSource { Kind = CertificateSourceKind.None });

        Assert.Null(result);
    }
}
