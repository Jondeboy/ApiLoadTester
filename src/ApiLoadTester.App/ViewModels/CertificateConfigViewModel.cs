using System.IO;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using ApiLoadTester.App.Services;
using ApiLoadTester.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace ApiLoadTester.App.ViewModels;

public partial class CertificateConfigViewModel : ObservableObject
{
    [ObservableProperty]
    private CertificateSourceKind _kind = CertificateSourceKind.None;

    [ObservableProperty]
    private string? _pfxFilePath;

    [ObservableProperty]
    private string? _storeThumbprintOrSubject;

    [ObservableProperty]
    private bool _rememberPassword;

    /// <summary>Raised when a scenario load recovers a DPAPI-decrypted password, so the view's
    /// code-behind can pre-fill the PasswordBox. Not raised on a fresh/manual certificate selection.</summary>
    public event Action<SecureString>? PasswordLoaded;

    /// <summary>
    /// The certificate password is intentionally NOT an ObservableProperty - binding a PasswordBox's
    /// plaintext into a view-model property would defeat the point of SecureString (it becomes an
    /// ordinary, un-zeroable managed string). Instead the view's code-behind wires this delegate to
    /// read PasswordBox.SecurePassword directly, only at the moment the password is actually needed.
    /// </summary>
    public Func<SecureString?>? PasswordProvider { get; set; }

    [RelayCommand]
    private void BrowsePfxFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select client certificate",
            InitialDirectory = CertificateFolderLocator.GetDefaultCertificatesFolder(),
            Filter = "Certificate files (*.pfx;*.p12)|*.pfx;*.p12|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            PfxFilePath = dialog.FileName;
            Kind = CertificateSourceKind.PfxFile;
        }
    }

    [RelayCommand]
    private void PickFromWindowsStore()
    {
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);

        var selected = X509Certificate2UI.SelectFromCollection(
            store.Certificates, "Select client certificate",
            "Choose the certificate to present for mutual TLS authentication.",
            X509SelectionFlag.SingleSelection);

        if (selected.Count > 0)
        {
            StoreThumbprintOrSubject = selected[0].Thumbprint;
            Kind = CertificateSourceKind.WindowsStore;
        }
    }

    public CertificateSource ToSource() => new()
    {
        Kind = Kind,
        PfxFilePath = Kind == CertificateSourceKind.PfxFile ? PfxFilePath : null,
        StoreThumbprintOrSubject = Kind == CertificateSourceKind.WindowsStore ? StoreThumbprintOrSubject : null,
        Password = Kind == CertificateSourceKind.PfxFile ? PasswordProvider?.Invoke() : null
    };

    public void LoadFrom(CertificateSource source, SecureString? decryptedPassword = null)
    {
        Kind = source.Kind;
        PfxFilePath = source.PfxFilePath;
        StoreThumbprintOrSubject = source.StoreThumbprintOrSubject;
        RememberPassword = decryptedPassword is not null;

        // A password is only present here if the scenario file had an opt-in "remember password"
        // DPAPI blob that decrypted successfully (see ScenarioSerializer.Load). Otherwise the user
        // re-enters it in the PasswordBox, which the view never surfaces back into this view model.
        if (decryptedPassword is not null)
            PasswordLoaded?.Invoke(decryptedPassword);
    }

    public string? Validate() => Kind switch
    {
        CertificateSourceKind.PfxFile when string.IsNullOrWhiteSpace(PfxFilePath) => "Select a .pfx certificate file.",
        CertificateSourceKind.PfxFile when !File.Exists(PfxFilePath) => "The selected certificate file no longer exists.",
        CertificateSourceKind.WindowsStore when string.IsNullOrWhiteSpace(StoreThumbprintOrSubject) => "Select a certificate from the Windows store.",
        _ => null
    };
}
