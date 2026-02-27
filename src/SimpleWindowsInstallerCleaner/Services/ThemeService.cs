using System.Windows;
using Microsoft.Win32;

namespace SimpleWindowsInstallerCleaner.Services;

internal static class ThemeService
{
    private static readonly Uri LightThemeUri = new("Themes/Light.xaml", UriKind.Relative);
    private static readonly Uri DarkThemeUri = new("Themes/Dark.xaml", UriKind.Relative);

    /// <summary>
    /// Detects the Windows theme and loads the matching resource dictionary.
    /// Call once at startup, before any windows are shown.
    /// </summary>
    internal static void ApplySystemTheme()
    {
        var isDark = IsSystemDarkTheme();
        var uri = isDark ? DarkThemeUri : LightThemeUri;

        var merged = Application.Current.Resources.MergedDictionaries;
        merged.Clear();
        merged.Add(new ResourceDictionary { Source = uri });
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            // 0 = dark, 1 = light, missing = assume light
            return value is int i && i == 0;
        }
        catch
        {
            return false; // assume light on failure
        }
    }
}
