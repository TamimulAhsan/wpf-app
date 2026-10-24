using System.Windows;

namespace PersonalExpenseTracker.Helpers;

/// <summary>Attached properties used by the custom TextBox template.</summary>
public static class TextBoxHelper
{
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.RegisterAttached("Placeholder", typeof(string), typeof(TextBoxHelper), new PropertyMetadata(string.Empty));

    public static string GetPlaceholder(DependencyObject element) => (string)element.GetValue(PlaceholderProperty);

    public static void SetPlaceholder(DependencyObject element, string value) => element.SetValue(PlaceholderProperty, value);
}
