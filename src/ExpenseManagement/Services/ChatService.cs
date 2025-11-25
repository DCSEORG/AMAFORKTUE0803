using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Data;
using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly IExpenseRepository _repository;
    private OpenAIClient? _openAIClient;
    private string? _deploymentName;
    private bool _isConfigured;

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger, IExpenseRepository repository)
    {
        _configuration = configuration;
        _logger = logger;
        _repository = repository;
        InitializeClient();
    }

    private void InitializeClient()
    {
        var endpoint = _configuration["OpenAI:Endpoint"];
        _deploymentName = _configuration["OpenAI:DeploymentName"] ?? "gpt-4o";

        if (string.IsNullOrEmpty(endpoint))
        {
            _logger.LogWarning("OpenAI endpoint not configured. Chat will return dummy responses.");
            _isConfigured = false;
            return;
        }

        try
        {
            var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
            Azure.Core.TokenCredential credential;
            
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                credential = new ManagedIdentityCredential(managedIdentityClientId);
            }
            else
            {
                _logger.LogInformation("Using DefaultAzureCredential");
                credential = new DefaultAzureCredential();
            }

            _openAIClient = new OpenAIClient(new Uri(endpoint), credential);
            _isConfigured = true;
            _logger.LogInformation("OpenAI client initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize OpenAI client");
            _isConfigured = false;
        }
    }

    public async Task<ChatResponse> ProcessMessageAsync(string message, List<ChatMessage>? history = null)
    {
        if (!_isConfigured || _openAIClient == null)
        {
            return new ChatResponse
            {
                Response = "The GenAI services are not currently deployed. To enable AI-powered chat functionality, please run the deploy-with-chat.sh script which will deploy Azure OpenAI and Cognitive Search services. Until then, you can still use the expense management features through the main interface.",
                Success = true
            };
        }

        try
        {
            var chatMessages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage(GetSystemPrompt())
            };

            // Add history if provided
            if (history != null)
            {
                foreach (var msg in history)
                {
                    if (msg.Role == "user")
                        chatMessages.Add(new ChatRequestUserMessage(msg.Content));
                    else if (msg.Role == "assistant")
                        chatMessages.Add(new ChatRequestAssistantMessage(msg.Content));
                }
            }

            chatMessages.Add(new ChatRequestUserMessage(message));

            // Define function tools for database operations
            var options = new ChatCompletionsOptions(_deploymentName, chatMessages)
            {
                Tools = {
                    new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
                    {
                        Name = "get_expenses",
                        Description = "Retrieves all expenses from the database with optional filters for status and category",
                        Parameters = BinaryData.FromObjectAsJson(new
                        {
                            type = "object",
                            properties = new
                            {
                                statusFilter = new { type = "string", description = "Filter by status: Draft, Submitted, Approved, or Rejected" },
                                categoryFilter = new { type = "string", description = "Filter by category: Travel, Meals, Supplies, Accommodation, or Other" }
                            }
                        })
                    }),
                    new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
                    {
                        Name = "get_pending_expenses",
                        Description = "Retrieves all expenses that are pending approval (status = Submitted)",
                        Parameters = BinaryData.FromObjectAsJson(new { type = "object", properties = new { } })
                    }),
                    new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
                    {
                        Name = "get_expense_summary",
                        Description = "Gets a summary of expenses grouped by status with counts and totals",
                        Parameters = BinaryData.FromObjectAsJson(new { type = "object", properties = new { } })
                    }),
                    new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
                    {
                        Name = "get_categories",
                        Description = "Gets the list of available expense categories",
                        Parameters = BinaryData.FromObjectAsJson(new { type = "object", properties = new { } })
                    }),
                    new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
                    {
                        Name = "create_expense",
                        Description = "Creates a new expense record",
                        Parameters = BinaryData.FromObjectAsJson(new
                        {
                            type = "object",
                            properties = new
                            {
                                categoryId = new { type = "integer", description = "Category ID (1=Travel, 2=Meals, 3=Supplies, 4=Accommodation, 5=Other)" },
                                amount = new { type = "number", description = "Amount in GBP" },
                                expenseDate = new { type = "string", description = "Date of expense in YYYY-MM-DD format" },
                                description = new { type = "string", description = "Description of the expense" }
                            },
                            required = new[] { "categoryId", "amount", "expenseDate" }
                        })
                    }),
                    new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
                    {
                        Name = "approve_expense",
                        Description = "Approves an expense by its ID",
                        Parameters = BinaryData.FromObjectAsJson(new
                        {
                            type = "object",
                            properties = new
                            {
                                expenseId = new { type = "integer", description = "The ID of the expense to approve" }
                            },
                            required = new[] { "expenseId" }
                        })
                    }),
                    new ChatCompletionsFunctionToolDefinition(new FunctionDefinition
                    {
                        Name = "reject_expense",
                        Description = "Rejects an expense by its ID",
                        Parameters = BinaryData.FromObjectAsJson(new
                        {
                            type = "object",
                            properties = new
                            {
                                expenseId = new { type = "integer", description = "The ID of the expense to reject" }
                            },
                            required = new[] { "expenseId" }
                        })
                    })
                }
            };

            var response = await _openAIClient.GetChatCompletionsAsync(options);
            var choice = response.Value.Choices[0];

            // Handle function calling loop
            while (choice.FinishReason == CompletionsFinishReason.ToolCalls)
            {
                var toolCalls = choice.Message.ToolCalls;
                chatMessages.Add(new ChatRequestAssistantMessage(choice.Message));

                foreach (var toolCall in toolCalls)
                {
                    if (toolCall is ChatCompletionsFunctionToolCall functionToolCall)
                    {
                        var functionResult = await ExecuteFunctionAsync(functionToolCall.Name, functionToolCall.Arguments);
                        chatMessages.Add(new ChatRequestToolMessage(functionResult, functionToolCall.Id));
                    }
                }

                // Get next response
                options = new ChatCompletionsOptions(_deploymentName, chatMessages);
                response = await _openAIClient.GetChatCompletionsAsync(options);
                choice = response.Value.Choices[0];
            }

            return new ChatResponse
            {
                Response = choice.Message.Content ?? "I couldn't generate a response.",
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return new ChatResponse
            {
                Response = "I encountered an error processing your request. Please try again.",
                Success = false,
                Error = ex.Message
            };
        }
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string arguments)
    {
        try
        {
            var args = JsonDocument.Parse(arguments);

            switch (functionName)
            {
                case "get_expenses":
                    var statusFilter = args.RootElement.TryGetProperty("statusFilter", out var sf) ? sf.GetString() : null;
                    var categoryFilter = args.RootElement.TryGetProperty("categoryFilter", out var cf) ? cf.GetString() : null;
                    var (expenses, _) = await _repository.GetExpensesAsync(statusFilter, categoryFilter);
                    return JsonSerializer.Serialize(expenses.Select(e => new { e.ExpenseId, e.UserName, e.CategoryName, e.Amount, e.Currency, e.ExpenseDate, e.Description, e.StatusName }));

                case "get_pending_expenses":
                    var (pending, _) = await _repository.GetPendingExpensesAsync();
                    return JsonSerializer.Serialize(pending.Select(e => new { e.ExpenseId, e.UserName, e.CategoryName, e.Amount, e.Currency, e.ExpenseDate, e.Description }));

                case "get_expense_summary":
                    var (summary, _) = await _repository.GetExpenseSummaryAsync();
                    return JsonSerializer.Serialize(summary);

                case "get_categories":
                    var (categories, _) = await _repository.GetCategoriesAsync();
                    return JsonSerializer.Serialize(categories);

                case "create_expense":
                    var createRequest = new ExpenseCreateRequest
                    {
                        UserId = 1, // Default to first user
                        CategoryId = args.RootElement.GetProperty("categoryId").GetInt32(),
                        Amount = args.RootElement.GetProperty("amount").GetDecimal(),
                        ExpenseDate = DateTime.Parse(args.RootElement.GetProperty("expenseDate").GetString() ?? DateTime.Now.ToString("yyyy-MM-dd")),
                        Description = args.RootElement.TryGetProperty("description", out var desc) ? desc.GetString() : null
                    };
                    var (expenseId, createError) = await _repository.CreateExpenseAsync(createRequest);
                    return createError != null 
                        ? JsonSerializer.Serialize(new { success = false, error = createError })
                        : JsonSerializer.Serialize(new { success = true, expenseId });

                case "approve_expense":
                    var approveId = args.RootElement.GetProperty("expenseId").GetInt32();
                    var (approveSuccess, approveError) = await _repository.ApproveExpenseAsync(approveId, 2); // Default reviewer ID
                    return JsonSerializer.Serialize(new { success = approveSuccess, error = approveError });

                case "reject_expense":
                    var rejectId = args.RootElement.GetProperty("expenseId").GetInt32();
                    var (rejectSuccess, rejectError) = await _repository.RejectExpenseAsync(rejectId, 2); // Default reviewer ID
                    return JsonSerializer.Serialize(new { success = rejectSuccess, error = rejectError });

                default:
                    return JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private static string GetSystemPrompt()
    {
        return @"You are an AI assistant for the Expense Management System. You help users manage their expenses, 
including viewing, creating, and approving expenses.

You have access to the following functions:
- get_expenses: Retrieve expenses with optional status/category filters
- get_pending_expenses: Get expenses awaiting approval
- get_expense_summary: Get counts and totals by status
- get_categories: List available expense categories
- create_expense: Create a new expense
- approve_expense: Approve a pending expense
- reject_expense: Reject a pending expense

When listing expenses, format them clearly with:
- Date (DD/MM/YYYY format)
- Category
- Amount (in GBP with £ symbol)
- Status
- Description

Be helpful and concise. When users ask about expenses, retrieve the relevant data and present it in a user-friendly format.
Use markdown formatting for lists with numbered items (1. 2. 3.) or bullets (- or *) when appropriate.
Use **bold** for emphasis on important information like totals or status.";
    }
}
