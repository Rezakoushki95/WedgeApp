using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class JourneyService
{
    private readonly AppDbContext _context;
    private readonly ScoringService _scoring;

    public JourneyService(AppDbContext context, ScoringService scoring)
    {
        _context = context;
        _scoring = scoring;
    }

    public async Task<Journey> CreateJourney(CreateJourneyDTO dto)
    {
        var journey = new Journey
        {
            UserId = dto.UserId,
            Name = string.IsNullOrWhiteSpace(dto.Name) ? "Untitled Journey" : dto.Name.Trim(),
            PatternTag = dto.PatternTag.Trim(),
            IsSandbox = dto.IsSandbox,
        };
        _context.Journeys.Add(journey);
        await _context.SaveChangesAsync();
        return journey;
    }

    public async Task<Journey?> RenameJourney(int journeyId, string name)
    {
        var journey = await _context.Journeys.FindAsync(journeyId);
        if (journey == null) return null;
        journey.Name = string.IsNullOrWhiteSpace(name) ? journey.Name : name.Trim();
        await _context.SaveChangesAsync();
        return journey;
    }

    // Archiving only hides the folder; the journey's trades remain in the pool.
    public async Task<Journey?> SetArchived(int journeyId, bool archived)
    {
        var journey = await _context.Journeys.FindAsync(journeyId);
        if (journey == null) return null;
        journey.Archived = archived;
        await _context.SaveChangesAsync();
        return journey;
    }

    public async Task<List<JourneyStatsDTO>> GetJourneys(int userId, bool includeArchived = false)
    {
        var journeys = await _context.Journeys
            .Where(j => j.UserId == userId && (includeArchived || !j.Archived))
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        var result = new List<JourneyStatsDTO>();
        foreach (var j in journeys)
        {
            result.Add(await GetStats(j.Id) ?? EmptyStats(j));
        }
        return result;
    }

    public async Task<JourneyStatsDTO?> GetStats(int journeyId)
    {
        var journey = await _context.Journeys.FindAsync(journeyId);
        if (journey == null) return null;

        var trades = await _context.Trades
            .Where(t => t.JourneyId == journeyId)
            .ToListAsync();

        var stats = _scoring.ComputeStats(trades);
        stats.JourneyId = journey.Id;
        stats.Name = journey.Name;
        stats.PatternTag = journey.PatternTag;
        stats.IsSandbox = journey.IsSandbox;
        stats.Archived = journey.Archived;
        return stats;
    }

    // Records a trade. R is recomputed server-side from the original stop — the
    // client's claimed result is never trusted.
    public async Task<TradeResultDTO> SubmitTrade(SubmitTradeDTO dto)
    {
        var journey = await _context.Journeys.FindAsync(dto.JourneyId)
            ?? throw new KeyNotFoundException($"Journey {dto.JourneyId} not found.");

        var r = _scoring.ComputeR(dto.Direction, dto.EntryPrice, dto.OriginalStop, dto.ExitPrice);

        var trade = new Trade
        {
            JourneyId = dto.JourneyId,
            ChartId = dto.ChartId,
            Direction = dto.Direction,
            EntryBarIndex = dto.EntryBarIndex,
            EntryPrice = dto.EntryPrice,
            OriginalStop = dto.OriginalStop,
            LiveStop = dto.LiveStop,
            ExitBarIndex = dto.ExitBarIndex,
            ExitPrice = dto.ExitPrice,
            ExitReason = dto.ExitReason,
            ResultR = r,
            RiskFraction = dto.RiskFraction <= 0m ? 0.01m : dto.RiskFraction,
        };
        _context.Trades.Add(trade);
        await _context.SaveChangesAsync();

        var trades = await _context.Trades
            .Where(t => t.JourneyId == dto.JourneyId)
            .ToListAsync();

        return new TradeResultDTO
        {
            TradeId = trade.Id,
            ResultR = r,
            BankrollAfter = _scoring.ComputeBankroll(trades.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id)),
        };
    }

    private static JourneyStatsDTO EmptyStats(Journey j) => new()
    {
        JourneyId = j.Id,
        Name = j.Name,
        PatternTag = j.PatternTag,
        IsSandbox = j.IsSandbox,
        Archived = j.Archived,
        Bankroll = ScoringService.StartingBankroll,
        EdgeState = EdgeState.Calibrating,
    };
}
