using ADExplorer.Models;
using ADExplorer.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ADExplorer.Views;

public partial class ComputerView : UserControl
{
    private bool _isSearching;

    public AdService? AdService { get; set; }
    public int ResultCount { get; private set; }
    public event EventHandler<int>? ResultCountChanged;

    public ComputerView()
    {
        InitializeComponent();
        StatusText.Text = SearchStatus.Prompt;
        Loaded += (_, _) => SearchBox.Focus();
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e) => await RunSearchAsync();

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        await RunSearchAsync();
    }

    private async Task RunSearchAsync()
    {
        var service = AdService;
        var query = SearchBox.Text.Trim();
        if (service == null || query.Length == 0 || _isSearching) return;

        _isSearching = true;
        SearchButton.IsEnabled = false;
        Cursor = Cursors.Wait;
        StatusText.Text = SearchStatus.Searching;
        StatusText.Visibility = Visibility.Visible;
        DetailPanel.Visibility = Visibility.Collapsed;

        List<AdComputer> results;
        try
        {
            results = await Task.Run(() => service.SearchComputers(query));
            StatusText.Text = SearchStatus.ForResults(results.Count);
        }
        catch (Exception ex)
        {
            results = new();
            StatusText.Text = SearchStatus.ForError(ex);
        }
        finally
        {
            _isSearching = false;
            SearchButton.IsEnabled = true;
            Cursor = null;
        }

        ResultsList.ItemsSource = results;
        StatusText.Visibility = StatusText.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        ResultCount = results.Count;
        ResultCountChanged?.Invoke(this, results.Count);
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not AdComputer computer)
        {
            DetailPanel.Visibility = Visibility.Collapsed;
            return;
        }
        DetailName.Text         = computer.Name;
        DetailStatus.Text       = computer.StatusDisplay;
        DetailStatus.Foreground = (Brush)FindResource(computer.IsEnabled ? "SuccessBrush" : "ErrorBrush");
        DetailOS.Text           = computer.OperatingSystem;
        DetailOSVersion.Text    = computer.OperatingSystemVersion;
        DetailLastLogon.Text    = computer.LastLogonDisplay;
        DetailOU.Text           = computer.DistinguishedName;
        DetailDesc.Text         = computer.Description;
        DetailPanel.Visibility  = Visibility.Visible;
    }
}
