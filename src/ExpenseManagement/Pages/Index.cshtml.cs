using ExpenseManagement.Data;
using ExpenseManagement.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseRepository _repository;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IExpenseRepository repository, ILogger<IndexModel> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public List<ExpenseSummary> ExpenseSummary { get; set; } = new();
    public List<Expense> RecentExpenses { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        var (summary, summaryError) = await _repository.GetExpenseSummaryAsync();
        ExpenseSummary = summary;

        var (expenses, expenseError) = await _repository.GetExpensesAsync();
        RecentExpenses = expenses.Take(5).ToList();

        ErrorMessage = summaryError ?? expenseError;
    }
}
