namespace backend.Models;

public class User
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }

    // Overall statistics
    public decimal TotalProfit { get; set; }
    public int TotalOrders { get; set; }
    public int TotalTradingDays { get; set; }

    // Navigation property for the current TradingSession
    public TradingSession? CurrentTradingSession { get; set; }

    // Navigation properties for accessed data
    public ICollection<AccessedDay> AccessedDays { get; set; } = new List<AccessedDay>();
    public ICollection<AccessedMonth> AccessedMonths { get; set; } = new List<AccessedMonth>();
}

