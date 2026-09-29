using ADExplorer.Models;
using ADExplorer.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ADExplorer.Views;

public partial class GroupSearchView : UserControl
{
    private bool _isSearching;

    public AdService? AdService { get; set; }
    public int ResultCount { get; private set; }
    public event EventHandler<int>? ResultCountChanged;

    public GroupSearchView()
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

        List<AdGroup> results;
        try
        {
            results = await Task.Run(() => service.SearchGroups(query));
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
        if (ResultsList.SelectedItem is not AdGroup group)
        {
            DetailPanel.Visibility = Visibility.Collapsed;
            return;
        }
        DetailName.Text         = group.Name;
        DetailType.Text         = group.GroupType;
        DetailDesc.Text         = group.Description;
        MemberCountText.Text    = $"MEMBERS ({group.MemberCount})";
        MembersList.ItemsSource = group.Members;
        DetailOU.Text           = group.DistinguishedName;
        DetailPanel.Visibility  = Visibility.Visible;
    }
}
