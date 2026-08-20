using System.Windows.Controls;
using ApiLoadTester.App.ViewModels;

namespace ApiLoadTester.App.Views;

public partial class RequestConfigView : UserControl
{
    public string[] HttpMethods => RequestConfigViewModel.HttpMethods;

    public RequestConfigView() => InitializeComponent();
}
