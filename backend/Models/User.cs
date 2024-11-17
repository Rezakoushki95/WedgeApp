namespace backend.Models;

public class User
{
    public int Id { get; set; }

    // Basic info
    public string Username { get; set; } = string.Empty; // Ensure it's never null
    public string PasswordHash { get; set; } = string.Empty;

    // Overall statistics
    public decimal TotalProfit { get; set; } = 0;
    public int TotalOrders { get; set; } = 0;
    public int TotalTradingDays { get; set; } = 0;

    // Navigation property for the latest TradingSession (optional)
    public TradingSession? CurrentTradingSession { get; set; }

    // Navigation properties for accessed data
    public ICollection<AccessedDay> AccessedDays { get; set; } = new List<AccessedDay>();
    public ICollection<AccessedMonth> AccessedMonths { get; set; } = new List<AccessedMonth>();
}
