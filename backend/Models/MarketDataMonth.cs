namespace backend.Models;

public class MarketDataMonth
{
    public int Id { get; set; }
    public string Month { get; set; } = string.Empty;
    public ICollection<MarketDataDay> Days { get; set; } = new List<MarketDataDay>();

    // Collection of AccessedMonth entries for tracking
    public ICollection<AccessedMonth> AccessedMonths { get; set; } = new List<AccessedMonth>();
}
