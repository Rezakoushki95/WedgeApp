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
    public async Task<TradingSession?> GetSession(int userId, string instrument = "S&P 500")
    {
        // Fetch the active session for the user and instrument
        return await _context.TradingSessions
            .Where(s => s.UserId == userId && s.Instrument == instrument)
            .FirstOrDefaultAsync();
    }

    // Start a new session for the user
    public async Task<TradingSession> CreateSession(int userId)
    {
        // Check if the user exists
        if (!await _context.Users.AnyAsync(u => u.Id == userId))
        {
            throw new Exception($"User with ID {userId} does not exist.");
        }

        // Check if a session already exists
        var existingSession = await GetSession(userId);
        if (existingSession != null)
        {
            throw new Exception($"A trading session for user {userId} and instrument already exists.");
        }

        // Fetch an unaccessed day using the centralized method
        var firstUnaccessedDay = await GetUnaccessedDay(userId);
        if (firstUnaccessedDay == null)
        {
            throw new Exception("No available trading data found for this user.");
        }

        // Create a new trading session
        var newSession = new TradingSession
        {
            UserId = userId,
            Instrument = "S&P 500",
            TradingDay = firstUnaccessedDay.Date,
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
    // Update the session state
    public async Task UpdateSession(int sessionId, int? currentBarIndex = null, bool? hasOpenOrder = null,
                                    decimal? entryPrice = null, decimal? totalProfitLoss = null, int? totalOrders = null)
    {
        var session = await _context.TradingSessions.FindAsync(sessionId);

        if (session == null)
        {
            throw new Exception("Session not found.");
        }

        // Validation
        if (currentBarIndex.HasValue && currentBarIndex.Value < 0)
        {
            throw new ArgumentException("CurrentBarIndex cannot be negative.");
        }

        if (totalOrders.HasValue && totalOrders.Value < 0)
        {
            throw new ArgumentException("TotalOrders cannot be negative.");
        }

        if (entryPrice.HasValue && entryPrice.Value <= 0)
        {
            throw new ArgumentException("EntryPrice must be greater than zero.");
        }

        // Apply updates
        if (currentBarIndex.HasValue) session.CurrentBarIndex = currentBarIndex.Value;
        if (hasOpenOrder.HasValue) session.HasOpenOrder = hasOpenOrder.Value;
        if (entryPrice.HasValue) session.EntryPrice = entryPrice.Value;
        if (totalProfitLoss.HasValue) session.TotalProfitLoss = totalProfitLoss.Value;
        if (totalOrders.HasValue) session.TotalOrders = totalOrders.Value;

        await _context.SaveChangesAsync();
    }

    public async Task CompleteDay(int sessionId)
    {
        try
        {
            var session = await _context.TradingSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                throw new Exception("Session not found.");
            }

            var user = session.User;
            if (user == null)
            {
                throw new Exception("User not associated with the session.");
            }

            // Mark the current day as accessed
            var currentDayId = _context.MarketDataDays
                .Where(d => d.Date == session.TradingDay)
                .Select(d => d.Id)
                .FirstOrDefault();

            if (currentDayId == 0)
            {
                throw new Exception("Current trading day not found in MarketDataDays.");
            }

            await MarkDayAsAccessed(user.Id, currentDayId);

            // Update user stats using UserStatsService
            await _userStatsService.UpdateUserStats(user.Id, session.TotalProfitLoss, session.TotalOrders);

            // Prepare the session for the next trading day
            var nextDay = await GetUnaccessedDay(user.Id)
                           ?? throw new Exception("No unaccessed trading days available for the user.");

            session.TradingDay = nextDay.Date;
            session.CurrentBarIndex = 0;
            session.TotalProfitLoss = 0;
            session.TotalOrders = 0;
            session.EntryPrice = null;

            await _context.SaveChangesAsync();

            Console.WriteLine($"User stats updated for user {user.Id}. Session reset to next trading day.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CompleteDay: {ex.Message}");
            throw;
        }
    }

    private async Task MarkDayAsAccessed(int userId, int dayId)
    {
        var accessedDay = new AccessedDay
        {
            UserId = userId,
            MarketDataDayId = dayId
        };

        _context.AccessedDays.Add(accessedDay);
        await _context.SaveChangesAsync();
        Console.WriteLine($"Marked day {dayId} as accessed for user {userId}.");
    }

    // Check for unaccessed days
    public async Task<MarketDataDay?> GetUnaccessedDay(int userId)
    {
        var unaccessedMonths = await _context.MarketDataMonths
            .Where(m => !_context.AccessedMonths
                .Any(a => a.UserId == userId && a.MarketDataMonthId == m.Id))
            .Include(m => m.Days)
            .ThenInclude(d => d.FiveMinuteBars)
            .ToListAsync();

        if (!unaccessedMonths.Any())
        {
            Console.WriteLine("No unaccessed months available for this user.");
            return null;
        }

        var random = new Random();
        var randomMonth = unaccessedMonths[random.Next(unaccessedMonths.Count)];

        var unaccessedDays = randomMonth.Days
            .Where(d => !_context.AccessedDays.Any(ad => ad.UserId == userId && ad.MarketDataDayId == d.Id))
            .OrderBy(d => Guid.NewGuid())
            .ToList();

        if (!unaccessedDays.Any())
        {
            Console.WriteLine("No unaccessed days in the selected month.");
            return null;
        }

        return unaccessedDays.First();
    }

}
