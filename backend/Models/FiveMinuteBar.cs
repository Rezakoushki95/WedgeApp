using System.Text.Json.Serialization;

namespace backend.Models;

public class FiveMinuteBar
{
    public int Id { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }

    // Foreign key to MarketDataDay
    public int MarketDataDayId { get; set; }
    [JsonIgnore]
    public MarketDataDay MarketDataDay { get; set; } = null!;
}


