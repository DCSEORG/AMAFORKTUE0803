using ExpenseManagement.Data;
using ExpenseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class ApproveModel : PageModel
{
    private readonly IExpenseRepository _repository;
    private readonly ILogger<ApproveModel> _logger;

    public ApproveModel(IExpenseRepository repository, ILogger<ApproveModel> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public List<Expense> PendingExpenses { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public string? CategoryFilter { get; set; }

    public async Task OnGetAsync(string? category = null)
    {
        CategoryFilter = category;

        var (expenses, error) = await _repository.GetPendingExpensesAsync(category);
        PendingExpenses = expenses;

        var (categories, _) = await _repository.GetCategoriesAsync();
        Categories = categories;

        ErrorMessage = error;
    }

    public async Task<IActionResult> OnPostApproveAsync(int expenseId)
    {
        var (success, error) = await _repository.ApproveExpenseAsync(expenseId, 2); // Manager ID = 2
        
        if (!success)
        {
            ErrorMessage = error ?? "Failed to approve expense";
        }
        else
        {
            SuccessMessage = $"Expense #{expenseId} approved successfully";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int expenseId)
    {
        var (success, error) = await _repository.RejectExpenseAsync(expenseId, 2); // Manager ID = 2
        
        if (!success)
        {
            ErrorMessage = error ?? "Failed to reject expense";
        }
        else
        {
            SuccessMessage = $"Expense #{expenseId} rejected";
        }

        return RedirectToPage();
    }
}
