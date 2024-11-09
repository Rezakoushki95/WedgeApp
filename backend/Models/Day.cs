namespace backend.Models
{
    public class Day
    {
        public int Id { get; set; } // Unique identifier for each trading day
        public string? Date { get; set; } // The date of the trading day, e.g., "2024-10-07"
        public ICollection<FiveMinuteBar>? FiveMinuteBars { get; set; } // Collection of five-minute bars for this day

        // Navigation property back to TradingSession
        public int TradingSessionId { get; set; } // Foreign key to identify the session
        public TradingSession? TradingSession { get; set; } // Reference to the associated TradingSession
    }
}
