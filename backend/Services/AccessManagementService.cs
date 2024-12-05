using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class AccessManagementService
{
    private readonly AppDbContext _context;

    public AccessManagementService(AppDbContext context)
    {
        _context = context;
    }

    // Fetch unaccessed day for a user
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

    // Mark a specific day as accessed
    public async Task MarkDayAsAccessed(int userId, int dayId)
    {
        var isAlreadyAccessed = _context.AccessedDays
            .Any(ad => ad.UserId == userId && ad.MarketDataDayId == dayId);

        if (!isAlreadyAccessed)
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
    }

    // Mark a month as accessed if all its days are accessed
    public async Task MarkMonthAsGloballyAccessed(int userId, int monthId)
    {
        var month = await _context.MarketDataMonths
            .Include(m => m.Days)
            .FirstOrDefaultAsync(m => m.Id == monthId);

        if (month == null)
        {
            Console.WriteLine($"MarketDataMonth with ID {monthId} not found.");
            return; // Safely exit if the month doesn't exist
        }

        // Check if all days in the month are accessed
        var allDaysAccessed = month.Days.All(d =>
            _context.AccessedDays.Any(ad => ad.UserId == userId && ad.MarketDataDayId == d.Id));

        if (allDaysAccessed)
        {
            var isAlreadyMarked = await _context.AccessedMonths
                .AnyAsync(am => am.UserId == userId && am.MarketDataMonthId == monthId);

            if (!isAlreadyMarked)
            {
                try
                {
                    var accessedMonth = new AccessedMonth
                    {
                        UserId = userId,
                        MarketDataMonthId = monthId
                    };

                    _context.AccessedMonths.Add(accessedMonth);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"Successfully marked month {monthId} as globally accessed for user {userId}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while marking month {monthId} as globally accessed: {ex.Message}");
                    throw;
                }
            }
            else
            {
                Console.WriteLine($"Month {monthId} is already marked as globally accessed for user {userId}.");
            }
        }
        else
        {
            Console.WriteLine($"Not all days are accessed for month {monthId} by user {userId}. Cannot mark as globally accessed.");
        }
    }



    public async Task<bool> AreAllMonthsGloballyAccessed()
    {
        var unaccessedMonths = await _context.MarketDataMonths
            .Where(m => !_context.AccessedMonths.Any(am => am.MarketDataMonthId == m.Id))
            .ToListAsync();

        return !unaccessedMonths.Any(); // True if all months are globally accessed
    }

}
