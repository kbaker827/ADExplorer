using ADExplorer.Services;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Threading;

namespace ADExplorer;

public partial class App : Application
{
    private readonly ThemeService _themeService = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        _themeService.Apply(this);
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // SystemEvents is static: unsubscribe so it does not hold on to the app.
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        base.OnExit(e);
    }

    // Follow Windows light/dark mode and accent colour changes while running.
    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color)
            Dispatcher.BeginInvoke(() => _themeService.Apply(this));
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}",
            "AD Explorer", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
