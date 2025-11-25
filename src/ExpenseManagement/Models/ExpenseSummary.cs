namespace ExpenseManagement.Models;

public class ExpenseSummary
{
    public string StatusName { get; set; } = string.Empty;
    public int ExpenseCount { get; set; }
    public decimal TotalAmount { get; set; }
    
    public string FormattedTotal => $"£{TotalAmount:N2}";
}
