using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiLoadTester.App.ViewModels;

public partial class RunHistoryViewModel : ObservableObject
{
    public ObservableCollection<TestRunHistoryItem> Runs { get; } = new();

    [ObservableProperty]
    private TestRunHistoryItem? _selected;

    public void Add(TestRunHistoryItem item)
    {
        Runs.Insert(0, item);
        Selected = item;
    }
}
