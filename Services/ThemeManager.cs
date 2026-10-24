using System.Windows;

namespace PersonalExpenseTracker.Services;

public static class ThemeManager
{
    /// <summary>Replaces merged dictionary 0 with the selected theme; the theme dictionary must stay first.</summary>
    public static void Apply(bool dark)
    {
        var palette = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Themes/{(dark ? "Dark" : "Light")}.xaml", UriKind.Absolute)
        };
        Application.Current.Resources.MergedDictionaries[0] = palette;
    }
}
