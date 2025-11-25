using ExpenseManagement.Data;
using ExpenseManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseRepository _repository;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(IExpenseRepository repository, ILogger<ExpensesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all expenses with optional filters
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Expense>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses(
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] int? userId = null)
    {
        var (expenses, error) = await _repository.GetExpensesAsync(status, category, userId);
        
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error);
        }
        
        return Ok(expenses);
    }

    /// <summary>
    /// Gets a specific expense by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Expense), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Expense>> GetExpense(int id)
    {
        var (expense, error) = await _repository.GetExpenseByIdAsync(id);
        
        if (expense == null)
        {
            return NotFound(new { message = "Expense not found", error });
        }
        
        return Ok(expense);
    }

    /// <summary>
    /// Gets all pending expenses (for approval)
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<Expense>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Expense>>> GetPendingExpenses([FromQuery] string? category = null)
    {
        var (expenses, error) = await _repository.GetPendingExpensesAsync(category);
        
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error);
        }
        
        return Ok(expenses);
    }

    /// <summary>
    /// Creates a new expense
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateExpense([FromBody] ExpenseCreateRequest request)
    {
        var (expenseId, error) = await _repository.CreateExpenseAsync(request);
        
        if (error != null)
        {
            return BadRequest(new { message = "Failed to create expense", error });
        }
        
        return CreatedAtAction(nameof(GetExpense), new { id = expenseId }, new { expenseId });
    }

    /// <summary>
    /// Updates an existing expense
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateExpense(int id, [FromBody] ExpenseUpdateRequest request)
    {
        request.ExpenseId = id;
        var (success, error) = await _repository.UpdateExpenseAsync(request);
        
        if (!success)
        {
            return BadRequest(new { message = "Failed to update expense", error });
        }
        
        return Ok(new { message = "Expense updated successfully" });
    }

    /// <summary>
    /// Deletes an expense
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteExpense(int id)
    {
        var (success, error) = await _repository.DeleteExpenseAsync(id);
        
        if (!success)
        {
            return BadRequest(new { message = "Failed to delete expense", error });
        }
        
        return Ok(new { message = "Expense deleted successfully" });
    }

    /// <summary>
    /// Submits an expense for approval
    /// </summary>
    [HttpPost("{id}/submit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SubmitExpense(int id)
    {
        var (success, error) = await _repository.SubmitExpenseAsync(id);
        
        if (!success)
        {
            return BadRequest(new { message = "Failed to submit expense", error });
        }
        
        return Ok(new { message = "Expense submitted for approval" });
    }

    /// <summary>
    /// Approves an expense
    /// </summary>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ApproveExpense(int id, [FromQuery] int reviewerId = 2)
    {
        var (success, error) = await _repository.ApproveExpenseAsync(id, reviewerId);
        
        if (!success)
        {
            return BadRequest(new { message = "Failed to approve expense", error });
        }
        
        return Ok(new { message = "Expense approved" });
    }

    /// <summary>
    /// Rejects an expense
    /// </summary>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RejectExpense(int id, [FromQuery] int reviewerId = 2)
    {
        var (success, error) = await _repository.RejectExpenseAsync(id, reviewerId);
        
        if (!success)
        {
            return BadRequest(new { message = "Failed to reject expense", error });
        }
        
        return Ok(new { message = "Expense rejected" });
    }

    /// <summary>
    /// Gets expense summary statistics
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ExpenseSummary>>> GetExpenseSummary([FromQuery] int? userId = null)
    {
        var (summary, error) = await _repository.GetExpenseSummaryAsync(userId);
        
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error);
        }
        
        return Ok(summary);
    }
}
