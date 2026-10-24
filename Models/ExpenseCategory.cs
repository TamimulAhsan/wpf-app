using System.Collections.ObjectModel;
using System.Windows.Media;

namespace PersonalExpenseTracker.Models;

/// <summary>A spending category together with the colours used to display it.</summary>
public sealed class ExpenseCategory
{
    public const string AllName = "All categories";
    public const string OtherName = "Other";
    public const int MaxNameLength = 24;

    /// <summary>Colours offered when the user creates a category.</summary>
    public static readonly IReadOnlyList<string> Palette = new[]
    {
        "#C2410C", "#0E7490", "#4D7C0F", "#8B5E3C", "#A23B5A", "#5B6CB8", "#3D6F8E", "#8A7A1F",
    };

    public static readonly ExpenseCategory All = new(AllName, "#64748B");

    private static readonly ObservableCollection<ExpenseCategory> AvailableCategories = new()
    {
        new ExpenseCategory("Food", "#B7791F"),
        new ExpenseCategory("Travel", "#12877B"),
        new ExpenseCategory("Study", "#3868B8"),
        new ExpenseCategory("Entertainment", "#B04F77"),
        new ExpenseCategory("Shopping", "#7566B5"),
        new ExpenseCategory("Health", "#2F8F5E"),
        new ExpenseCategory(OtherName, "#64748B"),
    };

    /// <summary>The categories an expense can belong to. Changes when the user adds a category.</summary>
    public static readonly ReadOnlyObservableCollection<ExpenseCategory> Available = new(AvailableCategories);

    private ExpenseCategory(string name, string hexColor)
    {
        Color color = (Color)ColorConverter.ConvertFromString(hexColor);
        Name = name;
        HexColor = hexColor;
        AccentBrush = CreateFrozenBrush(color);
        TintBrush = CreateFrozenBrush(Color.FromArgb(0x26, color.R, color.G, color.B));
    }

    public string Name { get; }
    public string HexColor { get; }
    public Brush AccentBrush { get; }
    public Brush TintBrush { get; }

    public static ExpenseCategory Find(string? name) =>
        AvailableCategories.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? AvailableCategories.First(c => c.Name == OtherName);

    /// <summary>Returns an error message when the name cannot be used for a new category, otherwise null.</summary>
    public static string? ValidateName(string? name)
    {
        string trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0) return "Enter a category name.";
        if (trimmed.Length > MaxNameLength) return $"Use {MaxNameLength} characters or fewer.";
        if (trimmed.Equals(AllName, StringComparison.OrdinalIgnoreCase)
            || AvailableCategories.Any(c => c.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            return "A category with this name already exists.";
        return null;
    }

    /// <summary>The first palette colour not used yet (or the first one when all are taken).</summary>
    public static string SuggestColor() =>
        Palette.FirstOrDefault(hex => AvailableCategories.All(c => !c.HexColor.Equals(hex, StringComparison.OrdinalIgnoreCase)))
        ?? Palette[0];

    /// <summary>Adds a category just before "Other" so that "Other" always stays last.</summary>
    public static ExpenseCategory Add(string name, string hexColor)
    {
        string? error = ValidateName(name);
        if (error is not null) throw new ArgumentException(error, nameof(name));

        var category = new ExpenseCategory(name.Trim(), hexColor);
        int otherIndex = AvailableCategories.ToList().FindIndex(c => c.Name == OtherName);
        AvailableCategories.Insert(otherIndex < 0 ? AvailableCategories.Count : otherIndex, category);
        return category;
    }

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
