using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Controls;
using ApiLoadTester.App.ViewModels;

namespace ApiLoadTester.App.Views;

public partial class CertificateConfigView : UserControl
{
    public CertificateConfigView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is CertificateConfigViewModel oldVm)
            oldVm.PasswordLoaded -= OnPasswordLoaded;

        if (e.NewValue is CertificateConfigViewModel newVm)
        {
            // The password never lives on the view model as a bound string - it's read straight out
            // of the PasswordBox at the moment it's needed (see CertificateConfigViewModel.ToSource).
            newVm.PasswordProvider = () => PfxPasswordBox.SecurePassword;
            newVm.PasswordLoaded += OnPasswordLoaded;
        }
    }

    private void OnPasswordLoaded(SecureString password)
    {
        // WPF's PasswordBox only exposes a plaintext string setter, so pre-filling it from a
        // remembered (DPAPI-decrypted) password necessarily materializes it briefly here - the same
        // trust boundary as the user typing it in by hand.
        var ptr = Marshal.SecureStringToGlobalAllocUnicode(password);
        try
        {
            PfxPasswordBox.Password = Marshal.PtrToStringUni(ptr) ?? "";
        }
        finally
        {
            Marshal.ZeroFreeGlobalAllocUnicode(ptr);
        }
    }
}
