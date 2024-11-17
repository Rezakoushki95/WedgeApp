using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;
public class TradingSessionService
{
    private readonly AppDbContext _context;
    private readonly UserStatsService _userStatsService;

    public TradingSessionService(AppDbContext context, UserStatsService userStatsService)
    {
        _context = context;
        _userStatsService = userStatsService;
    }

    // Get an existing session for the user
    public async Task<TradingSession?> GetSession(int userId)
    {
        return await _context.TradingSessions
            .Where(s => s.UserId == userId && s.Instrument == "S&P 500")
            .FirstOrDefaultAsync();
    }

    // Start a new session for the user
    public async Task<TradingSession> StartSession(int userId)
    {
        // Fetch the next available day for the user
        var nextDay = await _context.MarketDataDays
            .Include(d => d.AccessedDays)
            .Where(d => !d.AccessedDays.Any(ad => ad.UserId == userId))
            .OrderBy(d => d.Date) // Ensures chronological order
            .FirstOrDefaultAsync();

        if (nextDay == null)
        {
            throw new Exception("No available trading data found for this user.");
        }

        // Mark the day as accessed
        var accessedDay = new AccessedDay
        {
            UserId = userId,
            MarketDataDayId = nextDay.Id
        };
        _context.AccessedDays.Add(accessedDay);

        // Create a new trading session
        var newSession = new TradingSession
        {
            UserId = userId,
            Instrument = "S&P 500",
            TradingDay = nextDay.Date,
            CurrentBarIndex = 0,
            HasOpenOrder = false,
            EntryPrice = null,
            TotalProfitLoss = 0,
            TotalOrders = 0
        };

        _context.TradingSessions.Add(newSession);
        await _context.SaveChangesAsync();

        return newSession;
    }

    // Update the session state
    public async Task UpdateSession(int sessionId, int? currentBarIndex = null, bool? hasOpenOrder = null,
                                    decimal? entryPrice = null, decimal? totalProfitLoss = null, int? totalOrders = null)
    {
        var session = await _context.TradingSessions.FindAsync(sessionId);

        if (session == null)
        {
            throw new Exception("Session not found.");
        }

        if (currentBarIndex.HasValue) session.CurrentBarIndex = currentBarIndex.Value;
        if (hasOpenOrder.HasValue) session.HasOpenOrder = hasOpenOrder.Value;
        if (entryPrice.HasValue) session.EntryPrice = entryPrice;
        if (totalProfitLoss.HasValue) session.TotalProfitLoss = totalProfitLoss.Value;
        if (totalOrders.HasValue) session.TotalOrders = totalOrders.Value;

        await _context.SaveChangesAsync();
    }
}
