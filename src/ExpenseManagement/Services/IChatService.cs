namespace ExpenseManagement.Services;

public interface IChatService
{
    Task<Models.ChatResponse> ProcessMessageAsync(string message, List<Models.ChatMessage>? history = null);
}
