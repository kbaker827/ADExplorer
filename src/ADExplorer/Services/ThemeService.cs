using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace ADExplorer.Services;

public class ThemeService
{
    private const string PersonalizeKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string LightThemeValueName = "AppsUseLightTheme";
    private const string DwmKeyPath = @"Software\Microsoft\Windows\DWM";
    private const string AccentColorValueName = "AccentColor";

    private static readonly Uri LightThemeUri = new("pack://application:,,,/Themes/LightTheme.xaml");
    private static readonly Uri DarkThemeUri = new("pack://application:,,,/Themes/DarkTheme.xaml");
    private static readonly Color DefaultAccent = Color.FromRgb(0, 120, 215); // Windows default blue

    private ResourceDictionary? _currentTheme;

    public bool IsLightMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
            // Missing value means an older Windows build, which is light by default.
            return key?.GetValue(LightThemeValueName) is not int value || value != 0;
        }
        catch
        {
            return true;
        }
    }

    public Color GetAccentColor()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(DwmKeyPath);
            if (key?.GetValue(AccentColorValueName) is int colorValue)
            {
                // Stored as 0xAABBGGRR
                var bytes = BitConverter.GetBytes(colorValue);
                return Color.FromRgb(bytes[0], bytes[1], bytes[2]);
            }
        }
        catch { }
        return DefaultAccent;
    }

    public void Apply(Application app)
    {
        var dict = new ResourceDictionary { Source = IsLightMode() ? LightThemeUri : DarkThemeUri };

        if (_currentTheme != null)
            app.Resources.MergedDictionaries.Remove(_currentTheme);
        app.Resources.MergedDictionaries.Add(dict);
        _currentTheme = dict;

        var accent = GetAccentColor();
        var accentBrush = new SolidColorBrush(accent);
        accentBrush.Freeze();
        app.Resources["AccentColor"] = accent;
        app.Resources["AccentBrush"] = accentBrush;
    }
}
