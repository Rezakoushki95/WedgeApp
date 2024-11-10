
namespace backend.Models;

public class AccessedDay
{
    public int Id { get; set; }
    public int UserId { get; set; } // Foreign key to identify the user
    public User User { get; set; } = null!; // Navigation property for the user
    public int MarketDataDayId { get; set; } // The day that was accessed

    public MarketDataDay MarketDataDay { get; set; } = null!; // Navigation property for the day
}



