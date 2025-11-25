using ExpenseManagement.Data;
using ExpenseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class ExpensesModel : PageModel
{
    private readonly IExpenseRepository _repository;
    private readonly ILogger<ExpensesModel> _logger;

    public ExpensesModel(IExpenseRepository repository, ILogger<ExpensesModel> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public List<Expense> Expenses { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<ExpenseStatus> Statuses { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? StatusFilter { get; set; }
    public string? CategoryFilter { get; set; }

    public async Task OnGetAsync(string? status = null, string? category = null)
    {
        StatusFilter = status;
        CategoryFilter = category;

        var (expenses, expenseError) = await _repository.GetExpensesAsync(status, category);
        Expenses = expenses;

        var (categories, _) = await _repository.GetCategoriesAsync();
        Categories = categories;

        var (statuses, _) = await _repository.GetStatusesAsync();
        Statuses = statuses;

        ErrorMessage = expenseError;
    }

    public async Task<IActionResult> OnPostSubmitAsync(int expenseId)
    {
        await _repository.SubmitExpenseAsync(expenseId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int expenseId)
    {
        await _repository.DeleteExpenseAsync(expenseId);
        return RedirectToPage();
    }
}
