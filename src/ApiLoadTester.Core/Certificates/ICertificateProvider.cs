using System.Security.Cryptography.X509Certificates;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Certificates;

public interface ICertificateProvider
{
    bool CanResolve(CertificateSourceKind kind);
    X509Certificate2 Resolve(CertificateSource source);
}
