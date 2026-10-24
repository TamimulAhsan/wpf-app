using System.Globalization;

namespace PersonalExpenseTracker.Services;

public static class ExpenseValidator
{
    /// <summary>Largest amount accepted for a single expense; keeps the running totals well inside decimal range.</summary>
    public const decimal MaxAmount = 1_000_000_000m;

    /// <summary>
    /// Parses a plain number such as 1200 or 1200.50 (or 1200,50 on a Hungarian system).
    /// Thousands separators are rejected on purpose so "12,5" can never silently become 125.
    /// </summary>
    public static bool TryParseAmount(string? input, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;

        const NumberStyles styles = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite
                                  | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        return decimal.TryParse(input, styles, CultureInfo.CurrentCulture, out amount)
            || decimal.TryParse(input, styles, CultureInfo.InvariantCulture, out amount);
    }
}
