using ExpenseManagement.Data;
using ExpenseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class AddExpenseModel : PageModel
{
    private readonly IExpenseRepository _repository;
    private readonly ILogger<AddExpenseModel> _logger;

    public AddExpenseModel(IExpenseRepository repository, ILogger<AddExpenseModel> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public List<Category> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public decimal Amount { get; set; }

    [BindProperty]
    public DateTime? ExpenseDate { get; set; }

    [BindProperty]
    public int CategoryId { get; set; }

    [BindProperty]
    public string? Description { get; set; }

    public async Task OnGetAsync()
    {
        var (categories, error) = await _repository.GetCategoriesAsync();
        Categories = categories;
        ErrorMessage = error;
        ExpenseDate = DateTime.Today;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var (categories, _) = await _repository.GetCategoriesAsync();
        Categories = categories;

        if (Amount <= 0)
        {
            ErrorMessage = "Amount must be greater than zero.";
            return Page();
        }

        if (!ExpenseDate.HasValue)
        {
            ErrorMessage = "Please select a date.";
            return Page();
        }

        var request = new ExpenseCreateRequest
        {
            UserId = 1, // Default to first user for demo
            CategoryId = CategoryId,
            Amount = Amount,
            ExpenseDate = ExpenseDate.Value,
            Description = Description
        };

        var (expenseId, error) = await _repository.CreateExpenseAsync(request);

        if (error != null)
        {
            ErrorMessage = error;
            return Page();
        }

        SuccessMessage = $"Expense #{expenseId} created successfully!";
        
        // Reset form
        Amount = 0;
        ExpenseDate = DateTime.Today;
        CategoryId = categories.FirstOrDefault()?.CategoryId ?? 1;
        Description = null;

        return Page();
    }
}
