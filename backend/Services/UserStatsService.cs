using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class UserStatsService
    {
        private readonly AppDbContext _context;

        public UserStatsService(AppDbContext context)
        {
            _context = context;
        }

        // Method to update total stats when a session is closed
        public async Task UpdateUserStats(int userId, decimal sessionProfit, int sessionOrders)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                throw new Exception("User not found.");
            }

            // Update cumulative stats
            user.TotalProfit += sessionProfit;
            user.TotalOrders += sessionOrders;
            user.TotalTradingDays += 1; // Increment trading days on session close

            await _context.SaveChangesAsync();
        }
    }
}
