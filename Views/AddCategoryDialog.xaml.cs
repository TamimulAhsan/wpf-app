using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Views;

public partial class AddCategoryDialog : Window
{
    private string _selectedColor = ExpenseCategory.SuggestColor();

    private AddCategoryDialog()
    {
        InitializeComponent();
        BuildSwatches();
        UpdatePreview();
    }

    private string CategoryName => NameTextBox.Text.Trim();

    /// <summary>Shows the dialog modally; returns the chosen name and colour, or null when cancelled.</summary>
    public static (string Name, string ColorHex)? Show(Window owner)
    {
        var dialog = new AddCategoryDialog { Owner = owner };
        return dialog.ShowDialog() == true ? (dialog.CategoryName, dialog._selectedColor) : null;
    }

    private void BuildSwatches()
    {
        foreach (string hex in ExpenseCategory.Palette)
        {
            var swatch = new RadioButton
            {
                GroupName = "CategoryColour",
                Tag = hex,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)),
                IsChecked = hex.Equals(_selectedColor, StringComparison.OrdinalIgnoreCase),
                ToolTip = hex,
            };
            swatch.SetResourceReference(StyleProperty, "SwatchRadioButton");
            swatch.Checked += (_, _) =>
            {
                _selectedColor = (string)swatch.Tag;
                UpdatePreview();
            };
            SwatchPanel.Children.Add(swatch);
        }
    }

    private void UpdatePreview()
    {
        Color color = (Color)ColorConverter.ConvertFromString(_selectedColor);
        var accent = new SolidColorBrush(color);
        PreviewDot.Fill = accent;
        PreviewTextBlock.Foreground = accent;
        PreviewBorder.Background = new SolidColorBrush(Color.FromArgb(0x26, color.R, color.G, color.B));
        PreviewTextBlock.Text = CategoryName.Length == 0 ? "Category name" : CategoryName;
    }

    private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        NameTextBox.Tag = null;
        NameErrorTextBlock.Visibility = Visibility.Collapsed;
        UpdatePreview();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        string? error = ExpenseCategory.ValidateName(NameTextBox.Text);
        if (error is not null)
        {
            NameErrorTextBlock.Text = error;
            NameErrorTextBlock.Visibility = Visibility.Visible;
            NameTextBox.Tag = "Error";
            NameTextBox.Focus();
            return;
        }

        DialogResult = true;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) => NameTextBox.Focus();

    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource is not TextBox) DragMove();
    }
}
