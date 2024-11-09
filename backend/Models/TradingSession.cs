namespace backend.Models;
public class TradingSession
{
    public int Id { get; set; } // Unique identifier for each session
    public int UserId { get; set; } // Foreign key to identify the user
    public string Instrument { get; set; } = "S&P 500"; // e.g., "S&P 500"
    public string? TradingDay { get; set; } // The specific date of the trading day data

    // Progress tracking
    public int CurrentBarIndex { get; set; } // The current five-minute bar the user is on

    // Open order tracking
    public bool HasOpenOrder { get; set; }
    public decimal? EntryPrice { get; set; }
    public decimal? CurrentProfitLoss { get; set; } // Profit/loss for the current open order

    // Daily session summary
    public decimal TotalProfitLoss { get; set; } // Total profit/loss for the day
    public int TotalOrders { get; set; } // Number of orders opened within this day

    // Navigation properties
    public User? User { get; set; } // Reference back to the user
    public ICollection<Day>? Days { get; set; } // Collection of trading days in this session
}
