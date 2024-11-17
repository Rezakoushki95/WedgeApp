namespace backend.Models;

public class AccessedMonth
{
    public int Id { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public int MarketDataMonthId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public MarketDataMonth MarketDataMonth { get; set; } = null!;

}
