using ADExplorer.Models;
using ADExplorer.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ADExplorer.Views;

public partial class UserSearchView : UserControl
{
    private bool _isSearching;

    public AdService? AdService { get; set; }
    public int ResultCount { get; private set; }
    public event EventHandler<int>? ResultCountChanged;

    public UserSearchView()
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

        List<AdUser> results;
        try
        {
            results = await Task.Run(() => service.SearchUsers(query));
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

    private async void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not AdUser user)
        {
            DetailPanel.Visibility = Visibility.Collapsed;
            return;
        }
        PopulateDetail(user);

        // Re-read the selected user to get accurate lockout and password-expiry values,
        // which AD only computes for single-object reads.
        var service = AdService;
        if (service == null) return;
        var details = await Task.Run(() => service.GetUserDetails(user.DistinguishedName));
        if (details != null && ReferenceEquals(ResultsList.SelectedItem, user))
            PopulateDetail(details);
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
        DetailStatus.Foreground = (Brush)FindResource(user.IsEnabled ? "SuccessBrush" : "ErrorBrush");
        DetailLocked.Text  = user.LockedDisplay;
        DetailLocked.Foreground = (Brush)FindResource(user.IsLockedOut ? "WarningBrush" : "PrimaryText");
        DetailBadPwd.Text  = user.BadPasswordCount.ToString();
        DetailPwdExpiry.Text = user.PasswordExpiryDisplay();
        DetailLastLogon.Text = user.LastLogonDisplay;
        GroupsList.ItemsSource = user.Groups;
        DetailOU.Text      = user.DistinguishedName;
        DetailCreated.Text = user.Created.HasValue
            ? $"Created: {user.Created:yyyy-MM-dd}"
            : string.Empty;
        CopyEmailBtn.IsEnabled = !string.IsNullOrEmpty(user.Email);
        CopyUserBtn.IsEnabled  = !string.IsNullOrEmpty(user.SamAccountName);
        DetailPanel.Visibility = Visibility.Visible;
    }

    private void CopyEmail_Click(object sender, RoutedEventArgs e)
    {
        if (ResultsList.SelectedItem is AdUser user)
            CopyToClipboard(user.Email);
    }

    private void CopyUsername_Click(object sender, RoutedEventArgs e)
    {
        if (ResultsList.SelectedItem is AdUser user)
            CopyToClipboard(user.SamAccountName);
    }

    private static void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        try
        {
            Clipboard.SetText(text);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Another process is holding the clipboard open; nothing useful to do.
        }
    }
}
