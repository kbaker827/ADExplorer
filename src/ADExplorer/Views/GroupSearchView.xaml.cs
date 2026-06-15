using ADExplorer.Models;
using ADExplorer.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ADExplorer.Views;

public partial class GroupSearchView : UserControl
{
    public AdService? AdService { get; set; }
    public event EventHandler<int>? ResultCountChanged;

    public GroupSearchView()
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
        var results = AdService.SearchGroups(SearchBox.Text.Trim());
        ResultsList.ItemsSource = results;
        DetailPanel.Visibility = Visibility.Collapsed;
        ResultCountChanged?.Invoke(this, results.Count);
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not AdGroup group) return;
        DetailName.Text      = group.Name;
        DetailType.Text      = group.GroupType;
        DetailDesc.Text      = group.Description;
        MemberCountText.Text = $"MEMBERS ({group.MemberCount})";
        MembersList.ItemsSource = group.Members;
        DetailOU.Text        = group.DistinguishedName;
        DetailPanel.Visibility = Visibility.Visible;
    }
}
