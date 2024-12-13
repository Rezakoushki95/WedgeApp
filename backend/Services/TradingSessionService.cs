using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class TradingSessionService
{
    private readonly AppDbContext _context;
    private readonly UserStatsService _userStatsService;
    private readonly AccessManagementService _accessManagementService;
    private readonly MarketDataService _marketDataService;

    public TradingSessionService(AppDbContext context, UserStatsService userStatsService, AccessManagementService accessManagementService, MarketDataService markedataService)
    {
        _context = context;
        _userStatsService = userStatsService;
        _accessManagementService = accessManagementService;
        _marketDataService = markedataService;
    }
    public async Task<List<FiveMinuteBar>> GetBarsForSession(int sessionId)
    {
        // Fetch the session and ensure it exists
        var session = await _context.TradingSessions.FindAsync(sessionId);
        if (session == null)
        {
            throw new Exception("Session not found.");
        }

        // Fetch and return the bars for the trading day in the session
        return await _context.FiveMinuteBars
            .Where(bar => bar.MarketDataDay.Date == session.TradingDay)
            .OrderBy(bar => bar.Timestamp) // Ensure chronological order
            .ToListAsync();
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
        var firstUnaccessedDay = await _accessManagementService.GetUnaccessedDay(userId);
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
    public async Task<TradingSessionResponseDto> UpdateSession(
     int sessionId, int? currentBarIndex = null, bool? hasOpenOrder = null,
     decimal? entryPrice = null, decimal? totalProfitLoss = null, int? totalOrders = null)
    {
        var session = await _context.TradingSessions.FindAsync(sessionId);

        if (session == null)
        {
            throw new Exception("Session not found.");
        }

        // Validation
        if (currentBarIndex.HasValue && currentBarIndex.Value < 0)
            throw new ArgumentException("CurrentBarIndex cannot be negative.");
        if (totalOrders.HasValue && totalOrders.Value < 0)
            throw new ArgumentException("TotalOrders cannot be negative.");
        if (entryPrice.HasValue && entryPrice.Value <= 0)
            throw new ArgumentException("EntryPrice must be greater than zero.");

        // Apply updates
        if (currentBarIndex.HasValue) session.CurrentBarIndex = currentBarIndex.Value;
        if (hasOpenOrder.HasValue) session.HasOpenOrder = hasOpenOrder.Value;
        if (entryPrice.HasValue) session.EntryPrice = entryPrice.Value;
        if (totalProfitLoss.HasValue) session.TotalProfitLoss = totalProfitLoss.Value;
        if (totalOrders.HasValue) session.TotalOrders = totalOrders.Value;

        await _context.SaveChangesAsync();

        // Map the updated session to the response DTO
        return new TradingSessionResponseDto
        {
            SessionId = session.Id,
            Instrument = session.Instrument,
            TradingDay = session.TradingDay,
            CurrentBarIndex = session.CurrentBarIndex,
            HasOpenOrder = session.HasOpenOrder,
            EntryPrice = session.EntryPrice,
            TotalProfitLoss = session.TotalProfitLoss,
            TotalOrders = session.TotalOrders,
            OpenProfit = await CalculateOpenProfit(session)

        };
    }

    // Example of a helper method for computed fields
    private async Task<decimal> CalculateOpenProfit(TradingSession session)
    {
        // Validate session state
        if (!session.HasOpenOrder || session.EntryPrice == null)
            return 0;

        // Retrieve the current bar
        var currentBar = await _context.FiveMinuteBars
            .Where(bar => bar.MarketDataDay.Date == session.TradingDay)
            .OrderBy(bar => bar.Timestamp)
            .Skip(session.CurrentBarIndex) // Skip to the current bar index
            .FirstOrDefaultAsync();

        if (currentBar == null)
            throw new Exception("Current bar not found for the given trading day and index.");

        // Calculate profit based on the current price
        var currentPrice = currentBar.Close; // Use the closing price of the current bar
        return (currentPrice - session.EntryPrice.Value) * (session.HasOpenOrder ? 1 : -1);
    }


    public async Task CompleteDay(int sessionId)
    {
        var session = await _context.TradingSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            throw new Exception("Session not found.");
        }

        var user = session.User ?? throw new Exception("User not associated with the session.");

        // Mark the current day as accessed
        var currentDayId = _context.MarketDataDays
            .Where(d => d.Date == session.TradingDay)
            .Select(d => d.Id)
            .FirstOrDefault();

        await _accessManagementService.MarkDayAsAccessed(user.Id, currentDayId);

        // Mark the current month as globally accessed
        var currentMonthId = _context.MarketDataDays
            .Where(d => d.Id == currentDayId)
            .Select(d => d.MarketDataMonthId)
            .FirstOrDefault();

        if (currentMonthId != 0)
        {
            await _accessManagementService.MarkMonthAsGloballyAccessed(user.Id, currentMonthId);
        }

        // Update user stats
        await _userStatsService.UpdateUserStats(user.Id, session.TotalProfitLoss, session.TotalOrders);

        // Get next unaccessed day
        var nextDay = await _accessManagementService.GetUnaccessedDay(user.Id);

        if (nextDay == null)
        {
            // Check if all months are globally accessed
            var allMonthsGloballyAccessed = await _accessManagementService.AreAllMonthsGloballyAccessed();

            if (allMonthsGloballyAccessed)
            {
                // Fetch a new month only if the leading user has exhausted all available data
                await _marketDataService.FetchNextUniqueMonth();
            }

            // Retry getting the next day after potentially fetching new data
            nextDay = await _accessManagementService.GetUnaccessedDay(user.Id);

            if (nextDay == null)
            {
                throw new Exception("No available trading data even after fetching new month.");
            }
        }

        // Update session to the next day
        session.TradingDay = nextDay.Date;
        session.CurrentBarIndex = 0;
        session.TotalProfitLoss = 0;
        session.TotalOrders = 0;
        session.EntryPrice = null;

        await _context.SaveChangesAsync();
    }


}
