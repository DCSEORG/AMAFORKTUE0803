using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public interface IChatService
{
    Task<ChatResponse> ProcessMessageAsync(string message, List<ChatMessage>? history = null);
}
