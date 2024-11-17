namespace backend.Models;

public class AccessedDay
{
    public int Id { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public int MarketDataDayId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public MarketDataDay MarketDataDay { get; set; } = null!;


}
