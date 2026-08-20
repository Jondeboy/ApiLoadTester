using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiLoadTester.App.ViewModels;

public partial class HeaderEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _key = "";

    [ObservableProperty]
    private string _value = "";
}
