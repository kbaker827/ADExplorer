using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace ADExplorer.Services;

public class ThemeService
{
    private const string RegistryKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string RegistryValueName = "AppsUseLightTheme";

    public bool IsLightMode()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        var value = key?.GetValue(RegistryValueName);
        return value is int intValue && intValue == 1;
    }

    public Color GetAccentColor()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\DWM");
            var value = key?.GetValue("AccentColor");
            if (value is int colorValue)
            {
                var bytes = BitConverter.GetBytes(colorValue);
                return Color.FromRgb(bytes[0], bytes[1], bytes[2]);
            }
        }
        catch { }
        return Color.FromRgb(0, 120, 215); // Windows default blue
    }

    public void Apply(Application app)
    {
        var dict = new ResourceDictionary();
        if (IsLightMode())
        {
            dict.Source = new Uri("pack://application:,,,/Themes/LightTheme.xaml");
        }
        else
        {
            dict.Source = new Uri("pack://application:,,,/Themes/DarkTheme.xaml");
        }

        // Remove existing theme dict if present
        var existing = app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString?.Contains("Theme.xaml") == true);
        if (existing != null)
            app.Resources.MergedDictionaries.Remove(existing);

        app.Resources.MergedDictionaries.Add(dict);

        // Apply accent color
        var accent = GetAccentColor();
        app.Resources["AccentColor"] = accent;
        app.Resources["AccentBrush"] = new SolidColorBrush(accent);
    }
}
