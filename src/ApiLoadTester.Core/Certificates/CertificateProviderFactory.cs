using System.Security.Cryptography.X509Certificates;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Certificates;

public sealed class CertificateProviderFactory
{
    private readonly IReadOnlyList<ICertificateProvider> _providers;

    public CertificateProviderFactory()
        : this(new ICertificateProvider[] { new PfxFileCertificateProvider(), new WindowsStoreCertificateProvider() })
    {
    }

    public CertificateProviderFactory(IReadOnlyList<ICertificateProvider> providers)
    {
        _providers = providers;
    }

    public X509Certificate2? Resolve(CertificateSource source)
    {
        if (source.Kind == CertificateSourceKind.None)
            return null;

        var provider = _providers.FirstOrDefault(p => p.CanResolve(source.Kind))
            ?? throw new NotSupportedException($"No certificate provider registered for '{source.Kind}'.");

        return provider.Resolve(source);
    }
}
