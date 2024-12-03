using System.Text.Json.Serialization;

namespace backend.Models;

public class MarketDataDay
{
    public int Id { get; set; }

    // Store as DateTime for consistency
    public DateTime Date { get; set; }

    public ICollection<FiveMinuteBar> FiveMinuteBars { get; set; } = new List<FiveMinuteBar>();

    // Foreign key to the month
    public int MarketDataMonthId { get; set; }

    [JsonIgnore]
    public MarketDataMonth MarketDataMonth { get; set; } = null!;

    // Collection of AccessedDay entries for tracking
    [JsonIgnore]
    public ICollection<AccessedDay> AccessedDays { get; set; } = new List<AccessedDay>();

}

