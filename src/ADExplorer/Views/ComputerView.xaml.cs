using ADExplorer.Models;
using ADExplorer.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ADExplorer.Views;

public partial class ComputerView : UserControl
{
    public AdService? AdService { get; set; }
    public event EventHandler<int>? ResultCountChanged;

    public ComputerView()
    {
        InitializeComponent();
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e) => RunSearch();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) RunSearch();
    }

    private void RunSearch()
    {
        if (AdService == null || string.IsNullOrWhiteSpace(SearchBox.Text)) return;
        var results = AdService.SearchComputers(SearchBox.Text.Trim());
        ResultsList.ItemsSource = results;
        DetailPanel.Visibility = Visibility.Collapsed;
        ResultCountChanged?.Invoke(this, results.Count);
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not AdComputer computer) return;
        DetailName.Text        = computer.Name;
        DetailStatus.Text      = computer.StatusDisplay;
        DetailStatus.Foreground = computer.IsEnabled
            ? (Brush)FindResource("SuccessBrush")
            : (Brush)FindResource("ErrorBrush");
        DetailOS.Text          = computer.OperatingSystem;
        DetailOSVersion.Text   = computer.OperatingSystemVersion;
        DetailLastLogon.Text   = computer.LastLogonDisplay;
        DetailOU.Text          = computer.DistinguishedName;
        DetailDesc.Text        = computer.Description;
        DetailPanel.Visibility  = Visibility.Visible;
    }
}
