namespace backend.Models;

public class FiveMinuteBar
{
    public int Id { get; set; }
    public string? Date { get; set; } // The trading day date of this bar, e.g., "2024-10-07"
    public string? Time { get; set; } // Time within the trading day, e.g., "09:35:00"
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
}

