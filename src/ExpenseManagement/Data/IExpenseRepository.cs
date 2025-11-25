using ExpenseManagement.Models;

namespace ExpenseManagement.Data;

public interface IExpenseRepository
{
    // Expenses
    Task<(List<Expense> Expenses, string? Error)> GetExpensesAsync(string? statusFilter = null, string? categoryFilter = null, int? userId = null);
    Task<(Expense? Expense, string? Error)> GetExpenseByIdAsync(int expenseId);
    Task<(int ExpenseId, string? Error)> CreateExpenseAsync(ExpenseCreateRequest request);
    Task<(bool Success, string? Error)> UpdateExpenseAsync(ExpenseUpdateRequest request);
    Task<(bool Success, string? Error)> DeleteExpenseAsync(int expenseId);
    Task<(bool Success, string? Error)> SubmitExpenseAsync(int expenseId);
    Task<(bool Success, string? Error)> ApproveExpenseAsync(int expenseId, int reviewerId);
    Task<(bool Success, string? Error)> RejectExpenseAsync(int expenseId, int reviewerId);
    Task<(List<Expense> Expenses, string? Error)> GetPendingExpensesAsync(string? categoryFilter = null);
    
    // Lookups
    Task<(List<Category> Categories, string? Error)> GetCategoriesAsync();
    Task<(List<ExpenseStatus> Statuses, string? Error)> GetStatusesAsync();
    Task<(List<User> Users, string? Error)> GetUsersAsync();
    Task<(User? User, string? Error)> GetUserByIdAsync(int userId);
    Task<(List<ExpenseSummary> Summary, string? Error)> GetExpenseSummaryAsync(int? userId = null);
}
