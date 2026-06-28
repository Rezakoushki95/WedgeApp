using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

// Deals anonymized historical RTH charts and computes the magnet levels
// (prior day/week/month OHLC + opening gap) that give price-action context.
public class ChartService
{
    private readonly AppDbContext _context;

    public ChartService(AppDbContext context)
    {
        _context = context;
    }

    private readonly record struct DailyOhlc(
        DateTime Date, decimal Open, decimal High, decimal Low, decimal Close);

    public async Task<DealtChartDTO?> DealRandomChart()
    {
        var dayIds = await _context.MarketDataDays.Select(d => d.Id).ToListAsync();
        if (dayIds.Count == 0) return null;

        var pick = dayIds[new Random().Next(dayIds.Count)];
        return await DealChart(pick);
    }

    public async Task<DealtChartDTO?> DealChart(int chartId)
    {
        var day = await _context.MarketDataDays
            .Include(d => d.FiveMinuteBars)
            .FirstOrDefaultAsync(d => d.Id == chartId);

        if (day == null) return null;

        var orderedBars = day.FiveMinuteBars.OrderBy(b => b.Timestamp).ToList();
        var bars = orderedBars
            .Select((b, i) => new BarDTO
            {
                Index = i,
                Open = b.Open,
                High = b.High,
                Low = b.Low,
                Close = b.Close,
            })
            .ToList();

        return new DealtChartDTO
        {
            ChartId = day.Id,
            Bars = bars,
            Magnets = await ComputeMagnets(day),
        };
    }

    private async Task<MagnetLevelsDTO> ComputeMagnets(MarketDataDay day)
    {
        // Build a daily OHLC series across the whole stored dataset (single
        // instrument in V1). Fine for dev-scale data; revisit if it grows large.
        var allDays = await _context.MarketDataDays
            .Include(d => d.FiveMinuteBars)
            .ToListAsync();

        var dailies = allDays
            .Select(ToDailyOhlc)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .OrderBy(d => d.Date)
            .ToList();

        var today = dailies.FirstOrDefault(d => d.Date.Date == day.Date.Date);
        var magnets = new MagnetLevelsDTO();

        // Prior day = most recent day strictly before today.
        var prevDay = dailies.Where(d => d.Date.Date < day.Date.Date)
            .OrderByDescending(d => d.Date).Cast<DailyOhlc?>().FirstOrDefault();
        if (prevDay is { } pd)
        {
            magnets.PrevDayOpen = pd.Open;
            magnets.PrevDayHigh = pd.High;
            magnets.PrevDayLow = pd.Low;
            magnets.PrevDayClose = pd.Close;

            if (today.Open != 0m)
            {
                magnets.GapPoints = today.Open - pd.Close;
                magnets.GapPercent = pd.Close != 0m
                    ? (today.Open - pd.Close) / pd.Close * 100m
                    : null;
            }
        }

        // Prior calendar week (Mon–Sun) relative to the trading day's week.
        var startOfWeek = StartOfWeek(day.Date);
        var prevWeekDays = dailies
            .Where(d => d.Date >= startOfWeek.AddDays(-7) && d.Date < startOfWeek)
            .ToList();
        ApplyAggregate(prevWeekDays,
            (o, h, l, c) => { magnets.PrevWeekOpen = o; magnets.PrevWeekHigh = h; magnets.PrevWeekLow = l; magnets.PrevWeekClose = c; });

        // Prior calendar month.
        var firstOfMonth = new DateTime(day.Date.Year, day.Date.Month, 1);
        var prevMonthStart = firstOfMonth.AddMonths(-1);
        var prevMonthDays = dailies
            .Where(d => d.Date >= prevMonthStart && d.Date < firstOfMonth)
            .ToList();
        ApplyAggregate(prevMonthDays,
            (o, h, l, c) => { magnets.PrevMonthOpen = o; magnets.PrevMonthHigh = h; magnets.PrevMonthLow = l; magnets.PrevMonthClose = c; });

        return magnets;
    }

    private static void ApplyAggregate(List<DailyOhlc> period, Action<decimal, decimal, decimal, decimal> set)
    {
        if (period.Count == 0) return;
        var ordered = period.OrderBy(d => d.Date).ToList();
        set(
            ordered.First().Open,
            ordered.Max(d => d.High),
            ordered.Min(d => d.Low),
            ordered.Last().Close);
    }

    private static DailyOhlc? ToDailyOhlc(MarketDataDay day)
    {
        var bars = day.FiveMinuteBars.OrderBy(b => b.Timestamp).ToList();
        if (bars.Count == 0) return null;
        return new DailyOhlc(
            day.Date,
            bars.First().Open,
            bars.Max(b => b.High),
            bars.Min(b => b.Low),
            bars.Last().Close);
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        int diff = (7 + (int)date.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        return date.Date.AddDays(-diff);
    }
}
