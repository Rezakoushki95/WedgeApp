namespace backend.DTOs;

// A single candle, anonymized: a sequence index, never a real date/time.
public class BarDTO
{
    public int Index { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
}

// Prior open/high/low/close at day/week/month scale, plus the opening gap.
// Levels are nullable: at dataset edges some are not computable.
public class MagnetLevelsDTO
{
    public decimal? PrevDayOpen { get; set; }
    public decimal? PrevDayHigh { get; set; }
    public decimal? PrevDayLow { get; set; }
    public decimal? PrevDayClose { get; set; }

    public decimal? PrevWeekOpen { get; set; }
    public decimal? PrevWeekHigh { get; set; }
    public decimal? PrevWeekLow { get; set; }
    public decimal? PrevWeekClose { get; set; }

    public decimal? PrevMonthOpen { get; set; }
    public decimal? PrevMonthHigh { get; set; }
    public decimal? PrevMonthLow { get; set; }
    public decimal? PrevMonthClose { get; set; }

    // Today's open minus prior day's close (absolute and percent).
    public decimal? GapPoints { get; set; }
    public decimal? GapPercent { get; set; }
}

// A dealt chart: full RTH day of anonymized bars + magnet context. In V1 the
// whole day is sent and the client reveals bars one at a time (no rewind).
public class DealtChartDTO
{
    public int ChartId { get; set; }
    public List<BarDTO> Bars { get; set; } = new();
    public MagnetLevelsDTO Magnets { get; set; } = new();
}
