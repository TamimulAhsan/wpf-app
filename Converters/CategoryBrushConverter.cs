using System.Globalization;
using System.Windows.Data;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Converters;

/// <summary>Maps a category name to its accent brush, or to its tinted background when the parameter is "tint".</summary>
public sealed class CategoryBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        ExpenseCategory category = ExpenseCategory.Find(value as string);
        return string.Equals(parameter as string, "tint", StringComparison.OrdinalIgnoreCase)
            ? category.TintBrush
            : category.AccentBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
