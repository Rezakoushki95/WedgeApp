namespace backend.Models;

public class TradingSession
{
    public int Id { get; set; }

    // Foreign key for the user
    public int UserId { get; set; }
    public User? User { get; set; } // Navigation property to the user

    // Session-specific data
    public string Instrument { get; set; } = "S&P 500"; // Default instrument
    public DateTime? TradingDay { get; set; } // The trading day for the session

    public int CurrentBarIndex { get; set; } = 0; // Tracks bar progress within the day
    public bool HasOpenOrder { get; set; } = false; // Indicates if there’s an open order
    public decimal? EntryPrice { get; set; } = null; // Entry price for the current open order
    public decimal TotalProfitLoss { get; set; } = 0; // Total profit/loss in this session
    public int TotalOrders { get; set; } = 0; // Total orders executed in this session
}
