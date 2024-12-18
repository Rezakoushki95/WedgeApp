public class TradingSessionResponseDto
{
    public int SessionId { get; set; } // Primary key, always required
    public required string Instrument { get; set; } // Now required in model
    public required DateTime TradingDay { get; set; } // Now required in model
    public int CurrentBarIndex { get; set; } // Optional with default value
    public bool HasOpenOrder { get; set; }
    public decimal? EntryPrice { get; set; } // Nullable as before
    public decimal TotalProfitLoss { get; set; }
    public int TotalOrders { get; set; }
}
