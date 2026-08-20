using System.Security.Authentication;
using System.Windows.Controls;
using ApiLoadTester.App.ViewModels;

namespace ApiLoadTester.App.Views;

public partial class LoadOptionsView : UserControl
{
    public SslProtocols[] TlsProtocolOptions => LoadOptionsViewModel.TlsProtocolOptions;

    public LoadOptionsView() => InitializeComponent();
}
