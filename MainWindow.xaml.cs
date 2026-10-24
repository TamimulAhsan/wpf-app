using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PersonalExpenseTracker.Models;
using PersonalExpenseTracker.Services;
using PersonalExpenseTracker.Views;

namespace PersonalExpenseTracker;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<Expense> _expenses = new();
    private readonly ObservableCollection<CategorySummary> _categorySummaries = new();
    private readonly ICollectionView _expensesView;
    private readonly Dictionary<Control, TextBlock> _fieldErrors;
    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(3.5) };
    private int _nextId = 1;
    private bool _isDarkTheme;

    public MainWindow()
    {
        InitializeComponent();

        HeaderSubtitleTextBlock.Text = $"{DateTime.Today.ToString("dddd, d MMMM yyyy", CultureInfo.CurrentCulture)}  •  Keep everyday spending visible";

        CategoryComboBox.ItemsSource = ExpenseCategory.Available;
        CategoryComboBox.SelectedIndex = 0;
        // "All categories" followed by the live category list, so new categories show up in the filter immediately.
        FilterCategoryComboBox.ItemsSource = new CompositeCollection
        {
            ExpenseCategory.All,
            new CollectionContainer { Collection = ExpenseCategory.Available },
        };
        FilterCategoryComboBox.SelectedIndex = 0;
        ExpenseDatePicker.SelectedDate = DateTime.Today;
        CategorySummaryItemsControl.ItemsSource = _categorySummaries;

        _expensesView = CollectionViewSource.GetDefaultView(_expenses);
        _expensesView.Filter = ExpenseMatchesCurrentFilter;
        _expensesView.SortDescriptions.Add(new SortDescription(nameof(Expense.Date), ListSortDirection.Descending));
        _expensesView.SortDescriptions.Add(new SortDescription(nameof(Expense.Id), ListSortDirection.Descending));
        ExpensesDataGrid.ItemsSource = _expensesView;

        _fieldErrors = new Dictionary<Control, TextBlock>
        {
            [DescriptionTextBox] = DescriptionErrorTextBlock,
            [AmountTextBox] = AmountErrorTextBlock,
            [CategoryComboBox] = CategoryErrorTextBlock,
            [ExpenseDatePicker] = DateErrorTextBlock,
        };

        // Wired after initialisation so handlers never run against half-built state.
        DescriptionTextBox.TextChanged += (_, _) => ClearFieldError(DescriptionTextBox);
        AmountTextBox.TextChanged += (_, _) => ClearFieldError(AmountTextBox);
        CategoryComboBox.SelectionChanged += (_, _) => ClearFieldError(CategoryComboBox);
        ExpenseDatePicker.SelectedDateChanged += (_, _) => ClearFieldError(ExpenseDatePicker);
        DescriptionTextBox.KeyDown += FormTextBox_KeyDown;
        AmountTextBox.KeyDown += FormTextBox_KeyDown;
        FilterCategoryComboBox.SelectionChanged += (_, _) => RefreshExpenseList();
        SearchTextBox.TextChanged += (_, _) => RefreshExpenseList();
        _toastTimer.Tick += (_, _) => HideToast();

        RefreshExpenseList();
    }

    // ---------------------------------------------------------------- filtering

    private bool ExpenseMatchesCurrentFilter(object item)
    {
        if (item is not Expense expense) return false;

        bool categoryMatches = !IsCategoryFilterActive
            || expense.Category.Equals(SelectedFilterName, StringComparison.OrdinalIgnoreCase);

        string search = SearchTextBox.Text.Trim();
        bool textMatches = search.Length == 0
            || expense.Description.Contains(search, StringComparison.OrdinalIgnoreCase);

        return categoryMatches && textMatches;
    }

    private string SelectedFilterName => (FilterCategoryComboBox.SelectedItem as ExpenseCategory)?.Name ?? ExpenseCategory.AllName;

    private bool IsCategoryFilterActive => SelectedFilterName != ExpenseCategory.AllName;

    private bool IsAnyFilterActive => IsCategoryFilterActive || SearchTextBox.Text.Trim().Length > 0;

    // ---------------------------------------------------------------- add

    private void AddExpenseButton_Click(object sender, RoutedEventArgs e) => AddExpense();

    private void FormTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        AddExpense();
    }

    private void AddExpense()
    {
        if (!TryReadExpense(out Expense expense))
        {
            ShowToast("Please fix the highlighted fields.", isError: true);
            return;
        }

        _expenses.Add(expense);
        RefreshExpenseList();
        if (_expensesView.Contains(expense))
        {
            ExpensesDataGrid.SelectedItem = expense;
            ExpensesDataGrid.ScrollIntoView(expense);
        }
        ClearFormFields();
        ShowToast($"Added “{expense.Description}” — {expense.Amount:N2} HUF", isError: false);
    }

    /// <summary>Validates the form and builds an Expense; marks every invalid field and returns false otherwise.</summary>
    private bool TryReadExpense(out Expense expense)
    {
        expense = new Expense();
        ClearFieldErrors();
        Control? firstInvalid = null;

        void Fail(Control field, string message)
        {
            _fieldErrors[field].Text = message;
            _fieldErrors[field].Visibility = Visibility.Visible;
            field.Tag = "Error";
            firstInvalid ??= field;
        }

        string description = DescriptionTextBox.Text.Trim();
        if (description.Length == 0)
            Fail(DescriptionTextBox, "Please enter a description.");

        decimal amount = 0;
        if (!ExpenseValidator.TryParseAmount(AmountTextBox.Text, out amount))
        {
            Fail(AmountTextBox, "Enter a valid number, for example 1200 or 1200.50.");
        }
        else
        {
            amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
            if (amount <= 0)
                Fail(AmountTextBox, "The amount must be greater than zero.");
            else if (amount > ExpenseValidator.MaxAmount)
                Fail(AmountTextBox, $"The amount cannot exceed {ExpenseValidator.MaxAmount:N0} HUF.");
        }

        ExpenseCategory? category = CategoryComboBox.SelectedItem as ExpenseCategory;
        if (category is null)
            Fail(CategoryComboBox, "Please choose a category.");

        DateTime? date = ExpenseDatePicker.SelectedDate;
        if (date is null)
            Fail(ExpenseDatePicker, "Please choose a valid date.");

        if (firstInvalid is not null)
        {
            firstInvalid.Focus();
            return false;
        }

        expense = new Expense
        {
            Id = _nextId++,
            Description = description,
            Amount = amount,
            Category = category!.Name,
            Date = date!.Value.Date,
        };
        return true;
    }

    private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        (string Name, string ColorHex)? result = AddCategoryDialog.Show(this);
        if (result is null) return;

        ExpenseCategory category = ExpenseCategory.Add(result.Value.Name, result.Value.ColorHex);
        CategoryComboBox.SelectedItem = category;
        ShowToast($"Category “{category.Name}” added.", isError: false);
    }

    // ---------------------------------------------------------------- delete

    private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e) =>
        DeleteExpense(ExpensesDataGrid.SelectedItem as Expense);

    private void RowDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: Expense expense })
            DeleteExpense(expense);
    }

    private void ExpensesDataGrid_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete) return;
        e.Handled = true;
        DeleteExpense(ExpensesDataGrid.SelectedItem as Expense);
    }

    private void DeleteExpense(Expense? expense)
    {
        if (expense is null)
        {
            ShowToast("Select an expense from the table first.", isError: true);
            return;
        }

        bool confirmed = ConfirmDialog.Show(this, "Delete this expense?",
            $"“{expense.Description}” ({expense.Amount:N2} HUF) will be removed from your list.",
            "Delete", isDestructive: true);
        if (!confirmed) return;

        _expenses.Remove(expense);
        RefreshExpenseList();
        ShowToast("Expense deleted.", isError: false);
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_expenses.Count == 0)
        {
            ShowToast("There are no expenses to clear.", isError: true);
            return;
        }

        bool confirmed = ConfirmDialog.Show(this, "Clear all expenses?",
            $"All {_expenses.Count} expenses will be removed. This cannot be undone.", "Clear all", isDestructive: true);
        if (!confirmed) return;

        _expenses.Clear();
        RefreshExpenseList();
        ShowToast("All expenses cleared.", isError: false);
    }

    // ---------------------------------------------------------------- misc buttons

    private void LoadSampleButton_Click(object sender, RoutedEventArgs e)
    {
        (string Description, decimal Amount, string Category, int DaysAgo)[] samples =
        {
            ("Weekly groceries", 18450m, "Food", 0),
            ("Lunch with friends", 6200m, "Food", 1),
            ("Monthly bus pass", 9500m, "Travel", 2),
            ("Statistics textbook", 14900m, "Study", 3),
            ("Cinema tickets", 4800m, "Entertainment", 4),
            ("Pharmacy", 3250m, "Health", 5),
            ("Train to Budapest", 5400m, "Travel", 6),
            ("New headphones", 22990m, "Shopping", 7),
        };

        foreach (var sample in samples)
        {
            _expenses.Add(new Expense
            {
                Id = _nextId++,
                Description = sample.Description,
                Amount = sample.Amount,
                Category = sample.Category,
                Date = DateTime.Today.AddDays(-sample.DaysAgo),
            });
        }

        RefreshExpenseList();
        ShowToast($"Loaded {samples.Length} sample expenses.", isError: false);
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        ThemeManager.Apply(_isDarkTheme);
        ThemeIconTextBlock.Text = _isDarkTheme ? "" : "";
        ThemeTextTextBlock.Text = _isDarkTheme ? "Light mode" : "Dark mode";
    }

    private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Clear();
        FilterCategoryComboBox.SelectedIndex = 0;
    }

    private void ClearFormButton_Click(object sender, RoutedEventArgs e)
    {
        ClearFormFields();
        ClearFieldErrors();
    }

    private void ExpensesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = ExpensesDataGrid.SelectedItem is Expense;
        DeleteSelectedButton.IsEnabled = hasSelection;
        SelectionHintTextBlock.Text = ExpensesDataGrid.SelectedItem is Expense expense
            ? $"Selected: {expense.Description} — {expense.Amount:N2} HUF"
            : "Select an expense to delete it.";
    }

    // ---------------------------------------------------------------- refresh & totals

    /// <summary>Re-applies the filter and refreshes every total, panel and empty-state message.</summary>
    private void RefreshExpenseList()
    {
        _expensesView.Refresh();

        List<Expense> visible = _expensesView.Cast<Expense>().ToList();
        decimal overallTotal = CalculateTotal(_expenses);
        decimal visibleTotal = CalculateTotal(visible);

        OverallTotalTextBlock.Text = FormatMoney(overallTotal);
        ExpenseCountTextBlock.Text = $"{_expenses.Count} {(_expenses.Count == 1 ? "expense" : "expenses")}";

        FilteredTotalTextBlock.Text = FormatMoney(visibleTotal);
        FilteredCountTextBlock.Text = IsCategoryFilterActive
            ? $"{SelectedFilterName}  •  {visible.Count} of {_expenses.Count} shown"
            : $"{visible.Count} of {_expenses.Count} shown";

        AverageTextBlock.Text = FormatMoney(_expenses.Count == 0 ? 0m : overallTotal / _expenses.Count);

        UpdateCategorySummaries(overallTotal);
        UpdateEmptyState(visible.Count);
    }

    private static decimal CalculateTotal(IEnumerable<Expense> expenses) => expenses.Sum(expense => expense.Amount);

    private static string FormatMoney(decimal amount) => $"{amount:N2} HUF";

    private void UpdateCategorySummaries(decimal overallTotal)
    {
        _categorySummaries.Clear();
        foreach (var group in _expenses
                     .GroupBy(expense => ExpenseCategory.Find(expense.Category))
                     .Select(g => (Category: g.Key, Total: CalculateTotal(g), Count: g.Count()))
                     .OrderByDescending(g => g.Total))
        {
            double percent = overallTotal == 0 ? 0 : (double)(group.Total / overallTotal * 100m);
            _categorySummaries.Add(new CategorySummary(group.Category.Name, group.Total, percent, group.Count, group.Category.AccentBrush));
        }

        CategorySummaryEmptyTextBlock.Visibility = _categorySummaries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (_categorySummaries.Count == 0)
        {
            TopCategoryTextBlock.Text = "—";
            TopCategoryTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            TopCategoryAmountTextBlock.Text = "No data yet";
            return;
        }

        CategorySummary top = _categorySummaries[0];
        TopCategoryTextBlock.Text = top.Name;
        TopCategoryTextBlock.Foreground = top.AccentBrush;
        TopCategoryAmountTextBlock.Text = $"{top.TotalText}  •  {top.Percent:0}% of total";
    }

    private void UpdateEmptyState(int visibleCount)
    {
        EmptyStatePanel.Visibility = visibleCount == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (visibleCount != 0) return;

        if (_expenses.Count == 0)
        {
            EmptyStateTitleTextBlock.Text = "No expenses yet";
            EmptyStateMessageTextBlock.Text = "Add your first expense using the form on the left.";
        }
        else
        {
            EmptyStateTitleTextBlock.Text = "No expenses match your filters";
            EmptyStateMessageTextBlock.Text = IsAnyFilterActive
                ? "Try another category or search term, or clear the filters."
                : "Nothing to show.";
        }
    }

    // ---------------------------------------------------------------- form helpers

    private void ClearFormFields()
    {
        DescriptionTextBox.Clear();
        AmountTextBox.Clear();
        CategoryComboBox.SelectedIndex = 0;
        ExpenseDatePicker.SelectedDate = DateTime.Today;
        ClearFieldErrors();
        DescriptionTextBox.Focus();
    }

    private void ClearFieldError(Control field)
    {
        field.Tag = null;
        _fieldErrors[field].Visibility = Visibility.Collapsed;
    }

    private void ClearFieldErrors()
    {
        foreach (Control field in _fieldErrors.Keys)
            ClearFieldError(field);
    }

    // ---------------------------------------------------------------- toast

    private void ShowToast(string message, bool isError)
    {
        ToastTextBlock.Text = message;
        ToastIconTextBlock.Text = isError ? "" : "";
        ToastBorder.SetResourceReference(Border.BackgroundProperty, isError ? "DangerSoftBrush" : "SuccessSoftBrush");
        ToastBorder.SetResourceReference(Border.BorderBrushProperty, isError ? "DangerBrush" : "SuccessBrush");
        ToastIconTextBlock.SetResourceReference(TextBlock.ForegroundProperty, isError ? "DangerBrush" : "SuccessBrush");
        ToastTextBlock.SetResourceReference(TextBlock.ForegroundProperty, isError ? "DangerBrush" : "SuccessBrush");

        ToastBorder.Visibility = Visibility.Visible;
        ToastBorder.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));

        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void HideToast()
    {
        _toastTimer.Stop();
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
        fadeOut.Completed += (_, _) =>
        {
            if (!_toastTimer.IsEnabled) ToastBorder.Visibility = Visibility.Collapsed;
        };
        ToastBorder.BeginAnimation(OpacityProperty, fadeOut);
    }
}
