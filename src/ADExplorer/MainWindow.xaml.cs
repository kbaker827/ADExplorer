using ADExplorer.Services;
using System.Windows;

namespace ADExplorer;

public partial class MainWindow : Window
{
    private readonly AdService _adService = new();

    public MainWindow()
    {
        InitializeComponent();
        DomainText.Text = $"Connected to: {_adService.DomainName}";
        ResultCountText.Text = "Results: 0";

        UserView.AdService = _adService;
        GroupView.AdService = _adService;
        ComputerView.AdService = _adService;
    }

    private void OnResultCountChanged(object sender, int count)
    {
        ResultCountText.Text = $"Results: {count}";
    }
}