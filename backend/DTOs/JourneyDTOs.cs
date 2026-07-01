using backend.Models;

namespace backend.DTOs;

public class CreateJourneyDTO
{
    public int UserId { get; set; } = 1;
    public string Name { get; set; } = "Untitled Journey";
    public string PatternTag { get; set; } = string.Empty;
    public bool IsSandbox { get; set; } = false;
}

public class SubmitTradeDTO
{
    public int JourneyId { get; set; }
    public int ChartId { get; set; }
    public TradeDirection Direction { get; set; }

    public int EntryBarIndex { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal OriginalStop { get; set; }
    public decimal? LiveStop { get; set; }

    public int ExitBarIndex { get; set; }
    public decimal ExitPrice { get; set; }
    public ExitReason ExitReason { get; set; }

    public decimal RiskFraction { get; set; } = 0.01m;
}

public class TradeResultDTO
{
    public int TradeId { get; set; }
    public decimal ResultR { get; set; }
    public decimal BankrollAfter { get; set; }
}

// "Edge" confidence states for a journey/pattern.
public enum EdgeState
{
    Calibrating, // not enough sample yet
    Noise,       // matured but no positive edge (could be luck / losing)
    Promising,   // positive but modest confidence
    Established  // positive edge with confidence
}

public class JourneyStatsDTO
{
    public int JourneyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PatternTag { get; set; } = string.Empty;
    public bool IsSandbox { get; set; }
    public bool Archived { get; set; }

    public int TradeCount { get; set; }
    public int DistinctCharts { get; set; }

    // Dopamine number: compounding virtual account from $10,000.
    public decimal Bankroll { get; set; }

    // Skill numbers.
    public decimal Expectancy { get; set; }   // mean R per trade
    public decimal Edge { get; set; }          // mean R − 1.64·stderr (per chart-day)
    public EdgeState EdgeState { get; set; }

    public decimal WinRate { get; set; }
    public decimal AvgWinR { get; set; }
    public decimal AvgLossR { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal MaxDrawdownR { get; set; }
}
