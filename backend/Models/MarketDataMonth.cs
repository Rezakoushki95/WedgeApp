namespace backend.Models;

public class MarketDataMonth
{
    public int Id { get; set; }

    // Store as a DateTime or split into Year and Month
    public DateTime Month { get; set; }

    public ICollection<MarketDataDay> Days { get; set; } = new List<MarketDataDay>();

    // Tracks user-specific access to the month
    public ICollection<AccessedMonth> AccessedMonths { get; set; } = new List<AccessedMonth>();

    // Helper method to check if the month is fully accessed
    public bool IsFullyAccessed(int userId) =>
        Days.All(day => day.AccessedDays.Any(ad => ad.UserId == userId));
}
