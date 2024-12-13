namespace backend.DTOs;

public class TradingSessionResponseDto
{
    public int SessionId { get; set; }
    public required string Instrument { get; set; }
    public DateTime TradingDay { get; set; }
    public int CurrentBarIndex { get; set; }
    public bool HasOpenOrder { get; set; }
    public decimal? EntryPrice { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public int TotalOrders { get; set; }
    public decimal OpenProfit { get; set; } // Example of a computed field
}
