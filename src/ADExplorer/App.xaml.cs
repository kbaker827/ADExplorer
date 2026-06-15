using ADExplorer.Services;
using System.Windows;

namespace ADExplorer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var themeService = new ThemeService();
        themeService.Apply(this);
    }
}

