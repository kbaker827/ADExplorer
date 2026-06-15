using ADExplorer.Models;
using ADExplorer.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ADExplorer.Views;

public partial class UserSearchView : UserControl
{
    public AdService? AdService { get; set; }
    public event EventHandler<int>? ResultCountChanged;

    public UserSearchView()
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
        var results = AdService.SearchUsers(SearchBox.Text.Trim());
        ResultsList.ItemsSource = results;
        DetailPanel.Visibility = Visibility.Collapsed;
        ResultCountChanged?.Invoke(this, results.Count);
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not AdUser user) return;
        PopulateDetail(user);
    }

    private void PopulateDetail(AdUser user)
    {
        DetailName.Text    = user.DisplayName;
        DetailEmail.Text   = user.Email;
        DetailTitle.Text   = user.Title;
        DetailDept.Text    = user.Department;
        DetailManager.Text = user.Manager;
        DetailPhone.Text   = user.Phone;
        DetailStatus.Text  = user.StatusDisplay;
        DetailStatus.Foreground = user.IsEnabled
            ? (Brush)FindResource("SuccessBrush")
            : (Brush)FindResource("ErrorBrush");
        DetailLocked.Text    = user.LockedDisplay;
        DetailBadPwd.Text    = user.BadPasswordCount.ToString();
        DetailPwdExpiry.Text = user.PasswordExpiryDate.HasValue
            ? $"{user.PasswordExpiryDays()} days ({user.PasswordExpiryDate:yyyy-MM-dd})"
            : "No expiry";
        DetailLastLogon.Text = user.LastLogonDisplay;
        GroupsList.ItemsSource = user.Groups;
        DetailOU.Text      = user.DistinguishedName;
        DetailCreated.Text = user.Created.HasValue
            ? $"Created: {user.Created:yyyy-MM-dd}"
            : string.Empty;
        DetailPanel.Visibility = Visibility.Visible;
    }

    private void CopyEmail_Click(object sender, RoutedEventArgs e)
    {
        if (ResultsList.SelectedItem is AdUser user)
            Clipboard.SetText(user.Email);
    }

    private void CopyUsername_Click(object sender, RoutedEventArgs e)
    {
        if (ResultsList.SelectedItem is AdUser user)
            Clipboard.SetText(user.SamAccountName);
    }
}
