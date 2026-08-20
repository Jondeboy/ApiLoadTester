using System.IO;

namespace ApiLoadTester.App.Services;

/// <summary>
/// Resolves the default folder to point the certificate file picker at. Primary location is the
/// Certificates folder shipped next to the app (works identically for `dotnet run` and a published
/// single-file exe, since AppContext.BaseDirectory isn't relocated by single-file publish). Falls
/// back to a per-user AppData folder if the app directory isn't writable (e.g. installed to a
/// locked-down location by IT) - see Certificates/README.md for the enterprise tradeoff.
/// </summary>
public static class CertificateFolderLocator
{
    public static string GetDefaultCertificatesFolder()
    {
        var appLocal = Path.Combine(AppContext.BaseDirectory, "Certificates");
        if (Directory.Exists(appLocal) && IsWritable(appLocal))
            return appLocal;

        var userLocal = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ApiLoadTester", "Certificates");
        Directory.CreateDirectory(userLocal);
        return userLocal;
    }

    private static bool IsWritable(string directory)
    {
        try
        {
            var probe = Path.Combine(directory, $".write-check-{Guid.NewGuid():N}");
            File.WriteAllBytes(probe, []);
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }
}
