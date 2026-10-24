namespace PersonalExpenseTracker.Models;

public sealed class Expense
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    /// <summary>The date the expense occurred.</summary>
    public DateTime Date { get; set; }
}
