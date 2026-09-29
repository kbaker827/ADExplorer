using ADExplorer.Services;
using System.Windows;
using System.Windows.Controls;

namespace ADExplorer;

public partial class MainWindow : Window
{
    private readonly AdService _adService = new();

    public MainWindow()
    {
        InitializeComponent();
        DomainText.Text = _adService.IsConnected
            ? $"Connected to: {_adService.DomainName}"
            : "Not connected to a domain";
        ResultCountText.Text = "Results: 0";

        UserView.AdService = _adService;
        GroupView.AdService = _adService;
        ComputerView.AdService = _adService;
    }

    private void OnResultCountChanged(object? sender, int count)
    {
        // Only the visible tab drives the status bar.
        if (sender == (MainTabs.SelectedItem as TabItem)?.Content)
            ResultCountText.Text = $"Results: {count}";
    }

    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles up from the ListBoxes inside each tab; ignore those.
        if (!ReferenceEquals(e.OriginalSource, MainTabs)) return;

        var count = (MainTabs.SelectedItem as TabItem)?.Content switch
        {
            Views.UserSearchView v  => v.ResultCount,
            Views.GroupSearchView v => v.ResultCount,
            Views.ComputerView v    => v.ResultCount,
            _ => 0,
        };
        ResultCountText.Text = $"Results: {count}";
    }
}
