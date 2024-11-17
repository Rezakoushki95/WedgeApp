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
            if (sessionOrders < 0) // Orders cannot be negative, but profit can
            {
                throw new ArgumentException("Session orders cannot be negative.");
            }

            var user = await _context.Users.FindAsync(userId) ?? throw new Exception("User not found.");

            // Use a transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update cumulative stats
                user.TotalProfit += sessionProfit; // This can be negative
                user.TotalOrders += sessionOrders;
                user.TotalTradingDays += 1;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Console.WriteLine($"Concurrency conflict while updating stats for user ID {userId}: {ex.Message}");
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating stats for user ID {userId}: {ex.Message}");
                await transaction.RollbackAsync();
                throw;
            }
        }

    }
}
