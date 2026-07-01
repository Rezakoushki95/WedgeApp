namespace backend.Models;

// The immutable ledger. Trades are append-only and never deleted ("tombstoned"):
// archiving a journey hides the folder but its trades remain in the pool.
public class Trade
{
    public int Id { get; set; }

    public int JourneyId { get; set; }

    // References MarketDataDay.Id — the anonymized chart that was dealt.
    public int ChartId { get; set; }

    public TradeDirection Direction { get; set; }

    public int EntryBarIndex { get; set; }
    public decimal EntryPrice { get; set; }

    // The original stop defines 1R and is frozen at entry. R is always measured
    // against this distance, regardless of how the live stop is later moved.
    public decimal OriginalStop { get; set; }

    // The live stop may be moved during the trade; it only affects the exit,
    // never the risk denominator used for scoring.
    public decimal? LiveStop { get; set; }

    public int ExitBarIndex { get; set; }
    public decimal ExitPrice { get; set; }
    public ExitReason ExitReason { get; set; }

    // Result in R-multiples, recomputed server-side (never trusted from client).
    public decimal ResultR { get; set; }

    // Fraction of bankroll risked per trade (fixed fractional sizing). Default 1%.
    public decimal RiskFraction { get; set; } = 0.01m;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
