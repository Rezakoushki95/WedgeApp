using backend.Data;
using backend.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public class LeaderboardService
{
    private readonly AppDbContext _context;

    public LeaderboardService(AppDbContext context)
    {
        _context = context;
    }

    public (IEnumerable<LeaderboardDTO> Users, int TotalUsers) GetLeaderboard(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1)
        {
            throw new ArgumentException("Invalid pagination parameters.");
        }

        // Fetch data from the database
        var usersQuery = _context.Users
            .AsNoTracking()
            .Select(u => new
            {
                u.Username,
                TotalProfit = (double)u.TotalProfit, // Cast decimal to double
                u.TotalOrders,
                u.TotalTradingDays
            })
            .ToList(); // Fetch data into memory

        // Perform calculations on the client side
        List<LeaderboardDTO> usersWithProfitPerDay = usersQuery
            .Select(u => new LeaderboardDTO
            {
                Username = u.Username,
                TotalProfit = u.TotalProfit,
                TotalOrders = u.TotalOrders,
                TotalTradingDays = u.TotalTradingDays,
                ProfitPerDay = u.TotalTradingDays > 0 ? u.TotalProfit / u.TotalTradingDays : 0
            })
            .OrderByDescending(u => u.ProfitPerDay)
            .ToList();

        var totalUsers = usersWithProfitPerDay.Count;

        // Calculate Rank and apply pagination
        var users = usersWithProfitPerDay
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select((u, index) =>
            {
                u.Rank = (page - 1) * pageSize + index + 1;
                return u;
            })
            .ToList();

        return (Users: users, TotalUsers: totalUsers);
    }
}
