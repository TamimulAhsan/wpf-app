using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PersonalExpenseTracker.Views;

public partial class ConfirmDialog : Window
{
    private ConfirmDialog(string title, string message, string confirmText, bool isDestructive)
    {
        InitializeComponent();
        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
        ConfirmButton.Content = confirmText;
        ConfirmButton.SetResourceReference(StyleProperty, isDestructive ? "DangerSolidButton" : "PrimaryButton");
        IconBadge.SetResourceReference(Border.BackgroundProperty, isDestructive ? "DangerSoftBrush" : "AccentSoftBrush");
        IconGlyph.SetResourceReference(TextBlock.ForegroundProperty, isDestructive ? "DangerBrush" : "AccentBrush");
        IconGlyph.Text = isDestructive ? "" : "";
    }

    /// <summary>Shows the dialog modally and returns true when the user confirms.</summary>
    public static bool Show(Window owner, string title, string message, string confirmText, bool isDestructive)
    {
        var dialog = new ConfirmDialog(title, message, confirmText, isDestructive) { Owner = owner };
        return dialog.ShowDialog() == true;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) => CancelButton.Focus();

    private void ConfirmButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }
}
