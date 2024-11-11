namespace backend.Models;
public class TradingSession
{
    public int Id { get; set; }
    public int UserId { get; set; } // Foreign key for the user
    public User? User { get; set; } // Navigation property to the user

    // Session-specific data
    public bool IsActive { get; set; } = true; // Whether the session is currently active
    public string Instrument { get; set; } = "S&P 500";
    public string? TradingDay { get; set; } // The trading day for the session, e.g., "2024-10-30"

    public int CurrentBarIndex { get; set; } // Tracks bar progress within the day
    public bool HasOpenOrder { get; set; } // Indicates if there’s an open order
    public decimal? EntryPrice { get; set; } // Entry price for the current open order
    public decimal? CurrentProfitLoss { get; set; } // P&L of the open order
    public decimal TotalProfitLoss { get; set; } // Total profit/loss in this session
    public int TotalOrders { get; set; } // Total orders executed in this session
}

