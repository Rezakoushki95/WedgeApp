namespace backend.Models;

public class MarketDataMonth
{
    public int Id { get; set; }

    // Store as a DateTime or split into Year and Month
    public DateTime Month { get; set; }

    public ICollection<MarketDataDay> Days { get; set; } = new List<MarketDataDay>();
}
