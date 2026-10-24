using System.Windows.Media;

namespace PersonalExpenseTracker.Models;

/// <summary>Spending total for one category, used by the breakdown panel.</summary>
public sealed record CategorySummary(string Name, decimal Total, double Percent, int Count, Brush AccentBrush)
{
    public string TotalText => $"{Total:N2} HUF";
    public string DetailText => $"{Percent:0}%  •  {Count} {(Count == 1 ? "expense" : "expenses")}";
}
