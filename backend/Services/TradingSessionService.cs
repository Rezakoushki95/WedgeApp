using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;
public class TradingSessionService
{
    private readonly AppDbContext _context;

    public TradingSessionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TradingSession> StartNewSession(int userId)
    {
        // Check if there's an existing incomplete session
        var existingSession = await GetActiveSession(userId);
        if (existingSession != null)
        {
            return existingSession;
        }

        // Get the next available day and month for training
        var nextMonth = await _context.MarketDataMonths
            .Include(m => m.Days)
            .Where(m => !m.Days.Any(d => d.AccessedDays.Any(a => a.UserId == userId)))
            .OrderBy(m => m.Id) // Get the earliest month
            .FirstOrDefaultAsync();

        if (nextMonth == null || !nextMonth.Days.Any())
        {
            throw new Exception("No available trading data found for this user.");
        }

        var nextDay = nextMonth.Days.FirstOrDefault();
        if (nextDay == null)
        {
            throw new Exception("No available trading day found in the selected month.");
        }

        // Create a new trading session
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
            TotalOrders = 0
        };

        _context.TradingSessions.Add(newSession);
        await _context.SaveChangesAsync();

        return newSession;
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
    }
}
