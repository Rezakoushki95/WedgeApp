namespace backend.Models;

using System.ComponentModel.DataAnnotations;

public class TradingSession
{
    public int Id { get; set; } // Primary Key: Always required by default in EF Core.

    [Required] // Ensure API validation
    public int UserId { get; set; }
    public User? User { get; set; } // Nullable navigation property

    [Required] // Ensure instrument is always provided
    public string Instrument { get; set; } = "S&P 500";

    [Required] // Ensure trading day is always provided
    public DateTime TradingDay { get; set; }

    public int CurrentBarIndex { get; set; } = 0;
    public bool HasOpenOrder { get; set; } = false;
    public decimal? EntryPrice { get; set; } = null;
    public decimal TotalProfitLoss { get; set; } = 0;
    public int TotalOrders { get; set; } = 0;
}
