using System.Data;
using ExpenseManagement.Models;
using Microsoft.Data.SqlClient;

namespace ExpenseManagement.Data;

public class ExpenseRepository : IExpenseRepository
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExpenseRepository> _logger;

    public ExpenseRepository(IConfiguration configuration, ILogger<ExpenseRepository> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<(List<Expense> Expenses, string? Error)> GetExpensesAsync(string? statusFilter = null, string? categoryFilter = null, int? userId = null)
    {
        var expenses = new List<Expense>();
        
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_GetExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@StatusFilter", (object?)statusFilter ?? DBNull.Value);
            command.Parameters.AddWithValue("@CategoryFilter", (object?)categoryFilter ?? DBNull.Value);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
            
            return (expenses, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ExpenseRepository.cs", nameof(GetExpensesAsync));
            _logger.LogError(ex, "Error getting expenses: {Error}", error);
            return (GetDummyExpenses(), error);
        }
    }

    public async Task<(Expense? Expense, string? Error)> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_GetExpenseById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (MapExpense(reader), null);
            }
            
            return (null, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ExpenseRepository.cs", nameof(GetExpenseByIdAsync));
            _logger.LogError(ex, "Error getting expense by ID: {Error}", error);
            return (null, error);
        }
    }

    public async Task<(int ExpenseId, string? Error)> CreateExpenseAsync(ExpenseCreateRequest request)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_CreateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@Amount", request.Amount);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            
            var outputParam = new SqlParameter("@ExpenseId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            command.Parameters.Add(outputParam);

            await command.ExecuteNonQueryAsync();
            
            return ((int)outputParam.Value, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ExpenseRepository.cs", nameof(CreateExpenseAsync));
            _logger.LogError(ex, "Error creating expense: {Error}", error);
            return (0, error);
        }
    }

    public async Task<(bool Success, string? Error)> UpdateExpenseAsync(ExpenseUpdateRequest request)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_UpdateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@Amount", request.Amount);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return (Convert.ToInt32(result) > 0, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ExpenseRepository.cs", nameof(UpdateExpenseAsync));
            _logger.LogError(ex, "Error updating expense: {Error}", error);
            return (false, error);
        }
    }

    public async Task<(bool Success, string? Error)> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_DeleteExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            var result = await command.ExecuteScalarAsync();
            return (Convert.ToInt32(result) > 0, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ExpenseRepository.cs", nameof(DeleteExpenseAsync));
            _logger.LogError(ex, "Error deleting expense: {Error}", error);
            return (false, error);
        }
    }

    public async Task<(bool Success, string? Error)> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_SubmitExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            var result = await command.ExecuteScalarAsync();
            return (Convert.ToInt32(result) > 0, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ExpenseRepository.cs", nameof(SubmitExpenseAsync));
            _logger.LogError(ex, "Error submitting expense: {Error}", error);
            return (false, error);
        }
    }

    public async Task<(bool Success, string? Error)> ApproveExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_ApproveExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);

            var result = await command.ExecuteScalarAsync();
            return (Convert.ToInt32(result) > 0, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ExpenseRepository.cs", nameof(ApproveExpenseAsync));
            _logger.LogError(ex, "Error approving expense: {Error}", error);
            return (false, error);
        }
    }

    public async Task<(bool Success, string? Error)> RejectExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_RejectExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);

            var result = await command.ExecuteScalarAsync();
            return (Convert.ToInt32(result) > 0, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ExpenseRepository.cs", nameof(RejectExpenseAsync));
            _logger.LogError(ex, "Error rejecting expense: {Error}", error);
            return (false, error);
        }
    }

    public async Task<(List<Expense> Expenses, string? Error)> GetPendingExpensesAsync(string? categoryFilter = null)
    {
        var expenses = new List<Expense>();
        
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_GetPendingExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@CategoryFilter", (object?)categoryFilter ?? DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
            
            return (expenses, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ExpenseRepository.cs", nameof(GetPendingExpensesAsync));
            _logger.LogError(ex, "Error getting pending expenses: {Error}", error);
            return (GetDummyExpenses().Where(e => e.StatusName == "Submitted").ToList(), error);
        }
    }

    public async Task<(List<Category> Categories, string? Error)> GetCategoriesAsync()
    {
        var categories = new List<Category>();
        
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_GetCategories", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
            
            return (categories, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ExpenseRepository.cs", nameof(GetCategoriesAsync));
            _logger.LogError(ex, "Error getting categories: {Error}", error);
            return (GetDummyCategories(), error);
        }
    }

    public async Task<(List<ExpenseStatus> Statuses, string? Error)> GetStatusesAsync()
    {
        var statuses = new List<ExpenseStatus>();
        
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_GetStatuses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName"))
                });
            }
            
            return (statuses, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ExpenseRepository.cs", nameof(GetStatusesAsync));
            _logger.LogError(ex, "Error getting statuses: {Error}", error);
            return (GetDummyStatuses(), error);
        }
    }

    public async Task<(List<User> Users, string? Error)> GetUsersAsync()
    {
        var users = new List<User>();
        
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_GetUsers", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }
            
            return (users, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ExpenseRepository.cs", nameof(GetUsersAsync));
            _logger.LogError(ex, "Error getting users: {Error}", error);
            return (GetDummyUsers(), error);
        }
    }

    public async Task<(User? User, string? Error)> GetUserByIdAsync(int userId)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_GetUserById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (MapUser(reader), null);
            }
            
            return (null, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ExpenseRepository.cs", nameof(GetUserByIdAsync));
            _logger.LogError(ex, "Error getting user by ID: {Error}", error);
            return (null, error);
        }
    }

    public async Task<(List<ExpenseSummary> Summary, string? Error)> GetExpenseSummaryAsync(int? userId = null)
    {
        var summary = new List<ExpenseSummary>();
        
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("usp_GetExpenseSummary", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                summary.Add(new ExpenseSummary
                {
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
                    ExpenseCount = reader.GetInt32(reader.GetOrdinal("ExpenseCount")),
                    TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount"))
                });
            }
            
            return (summary, null);
        }
        catch (Exception ex)
        {
            var error = FormatError(ex, "ExpenseRepository.cs", nameof(GetExpenseSummaryAsync));
            _logger.LogError(ex, "Error getting expense summary: {Error}", error);
            return (GetDummySummary(), error);
        }
    }

    private static Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
            ReviewerName = reader.IsDBNull(reader.GetOrdinal("ReviewerName")) ? null : reader.GetString(reader.GetOrdinal("ReviewerName")),
            ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private static User MapUser(SqlDataReader reader)
    {
        return new User
        {
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
            RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
            ManagerId = reader.IsDBNull(reader.GetOrdinal("ManagerId")) ? null : reader.GetInt32(reader.GetOrdinal("ManagerId")),
            ManagerName = reader.IsDBNull(reader.GetOrdinal("ManagerName")) ? null : reader.GetString(reader.GetOrdinal("ManagerName")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private static string FormatError(Exception ex, string fileName, string methodName)
    {
        var error = $"Database Error in {fileName} -> {methodName}(): {ex.Message}";
        
        // Add specific guidance for managed identity issues
        if (ex.Message.Contains("Managed Identity") || 
            ex.Message.Contains("Unable to load") ||
            ex.Message.Contains("access token") ||
            ex.Message.Contains("AADSTS"))
        {
            error += " | FIX: Ensure the App Service has a User Assigned Managed Identity configured and the AZURE_CLIENT_ID environment variable is set to the Managed Identity's Client ID. Also verify the Managed Identity has been granted db_datareader, db_datawriter, and EXECUTE permissions on the database.";
        }
        else if (ex.Message.Contains("Login failed") || ex.Message.Contains("Cannot open database"))
        {
            error += " | FIX: Check connection string, database exists, and managed identity has access. Run the run-sql-dbrole.py script to grant permissions.";
        }
        
        return error;
    }

    // Dummy data for when database is unavailable
    private static List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new() { ExpenseId = 1, UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", CategoryId = 1, CategoryName = "Travel", StatusId = 2, StatusName = "Submitted", Amount = 120.00m, Currency = "GBP", ExpenseDate = new DateTime(2024, 1, 15), Description = "Taxi to client site" },
            new() { ExpenseId = 2, UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", CategoryId = 2, CategoryName = "Meals", StatusId = 2, StatusName = "Submitted", Amount = 69.00m, Currency = "GBP", ExpenseDate = new DateTime(2023, 1, 10), Description = "Client lunch" },
            new() { ExpenseId = 3, UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", CategoryId = 3, CategoryName = "Supplies", StatusId = 3, StatusName = "Approved", Amount = 99.50m, Currency = "GBP", ExpenseDate = new DateTime(2023, 12, 4), Description = "Office supplies" },
            new() { ExpenseId = 4, UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", CategoryId = 1, CategoryName = "Travel", StatusId = 3, StatusName = "Approved", Amount = 19.20m, Currency = "GBP", ExpenseDate = new DateTime(2023, 12, 18), Description = "Transport" }
        };
    }

    private static List<Category> GetDummyCategories()
    {
        return new List<Category>
        {
            new() { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new() { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new() { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new() { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new() { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }

    private static List<ExpenseStatus> GetDummyStatuses()
    {
        return new List<ExpenseStatus>
        {
            new() { StatusId = 1, StatusName = "Draft" },
            new() { StatusId = 2, StatusName = "Submitted" },
            new() { StatusId = 3, StatusName = "Approved" },
            new() { StatusId = 4, StatusName = "Rejected" }
        };
    }

    private static List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new() { UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", RoleId = 1, RoleName = "Employee", IsActive = true },
            new() { UserId = 2, UserName = "Bob Manager", Email = "bob.manager@example.co.uk", RoleId = 2, RoleName = "Manager", IsActive = true }
        };
    }

    private static List<ExpenseSummary> GetDummySummary()
    {
        return new List<ExpenseSummary>
        {
            new() { StatusName = "Draft", ExpenseCount = 1, TotalAmount = 7.99m },
            new() { StatusName = "Submitted", ExpenseCount = 1, TotalAmount = 25.40m },
            new() { StatusName = "Approved", ExpenseCount = 2, TotalAmount = 137.25m }
        };
    }
}
