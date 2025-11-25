using ExpenseManagement.Data;
using ExpenseManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IExpenseRepository _repository;

    public CategoriesController(IExpenseRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets all expense categories
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Category>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        var (categories, error) = await _repository.GetCategoriesAsync();
        
        if (error != null)
        {
            Response.Headers.Append("X-Error-Message", error);
        }
        
        return Ok(categories);
    }
}
