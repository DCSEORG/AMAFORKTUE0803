using ExpenseManagement.Data;
using ExpenseManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IExpenseRepository _repository;

    public UsersController(IExpenseRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets all users
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        var (users, error) = await _repository.GetUsersAsync();
        
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error);
        }
        
        return Ok(users);
    }

    /// <summary>
    /// Gets a specific user by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var (user, error) = await _repository.GetUserByIdAsync(id);
        
        if (user == null)
        {
            return NotFound(new { message = "User not found", error });
        }
        
        return Ok(user);
    }
}
