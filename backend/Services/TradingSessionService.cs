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
    public async Task<TradingSession> StartNewSession(int userId)
    {
        // Check if there's an existing session for the user and instrument
        var existingSession = await _context.TradingSessions
            .Where(s => s.UserId == userId && s.Instrument == "S&P 500")
            .FirstOrDefaultAsync();

        // If an active session exists, return it
        if (existingSession != null && existingSession.IsActive)
        {
            return existingSession;
        }

        // If an inactive session exists, close it before creating a new one
        if (existingSession != null && !existingSession.IsActive)
        {
            _context.TradingSessions.Remove(existingSession);
            await _context.SaveChangesAsync();
        }

        // Proceed to create a new session
        var nextMonth = await _context.MarketDataMonths
            .Include(m => m.Days)
            .Where(m => !m.Days.Any(d => d.AccessedDays.Any(a => a.UserId == userId)))
            .OrderBy(m => m.Id)
            .FirstOrDefaultAsync();

        if (nextMonth == null || !nextMonth.Days.Any())
        {
            throw new Exception("No available trading data found for this user.");
        }

        var nextDay = nextMonth.Days.FirstOrDefault();
        if (nextDay == null || string.IsNullOrEmpty(nextDay.Date))
        {
            throw new Exception("No available trading day found in the selected month.");
        }

        var newSession = new TradingSession
        {
            UserId = userId,
            Instrument = "S&P 500",
            TradingDay = nextDay.Date,
            CurrentBarIndex = 0,
            HasOpenOrder = false,
            EntryPrice = null,
            CurrentProfitLoss = 0,
            TotalProfitLoss = 0,
            TotalOrders = 0,
            IsActive = true
        };

        _context.TradingSessions.Add(newSession);
        await _context.SaveChangesAsync();

        return newSession;
    }


    public async Task UpdateSession(int sessionId, int? currentBarIndex = null, bool? hasOpenOrder = null,
                                decimal? entryPrice = null, decimal? currentProfitLoss = null,
                                decimal? totalProfitLoss = null, int? totalOrders = null)
    {
        var session = await _context.TradingSessions.FindAsync(sessionId);

        if (session == null)
        {
            throw new Exception("Session not found.");
        }

        if (currentBarIndex.HasValue) session.CurrentBarIndex = currentBarIndex.Value;
        if (hasOpenOrder.HasValue) session.HasOpenOrder = hasOpenOrder.Value;
        if (entryPrice.HasValue) session.EntryPrice = entryPrice;
        if (currentProfitLoss.HasValue) session.CurrentProfitLoss = currentProfitLoss;
        if (totalProfitLoss.HasValue) session.TotalProfitLoss = totalProfitLoss.Value;
        if (totalOrders.HasValue) session.TotalOrders = totalOrders.Value;

        await _context.SaveChangesAsync();
    }

    public async Task<TradingSession?> GetActiveSession(int userId, string instrument = "S&P 500")
    {
        return await _context.TradingSessions
            .Where(s => s.UserId == userId && s.Instrument == instrument && s.IsActive)
            .FirstOrDefaultAsync();
    }

    public async Task CloseSession(int sessionId)
    {
        var session = await _context.TradingSessions.FindAsync(sessionId);

        if (session == null)
        {
            throw new Exception("Session not found.");
        }

        session.IsActive = false;
        await _context.SaveChangesAsync();
        await _userStatsService.UpdateUserStats(session.UserId, session.TotalProfitLoss, session.TotalOrders);

    }
}
