using System.Windows;

namespace PersonalExpenseTracker.Services;

public static class ThemeManager
{
    /// <summary>Swaps the colour palette (merged dictionary 0) between the light and dark themes.</summary>
    public static void Apply(bool dark)
    {
        var palette = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Themes/{(dark ? "Dark" : "Light")}.xaml", UriKind.Absolute)
        };
        Application.Current.Resources.MergedDictionaries[0] = palette;
    }
}
