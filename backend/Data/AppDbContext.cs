using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Define DbSet properties for each model you want to store in the database
        public DbSet<User> Users { get; set; }
        public DbSet<FiveMinuteBar> FiveMinuteBars { get; set; }
        public DbSet<TradingSession> TradingSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed a sample user
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 5,
                Username = "testuser5",
                PasswordHash = "75K3eLr+dx6JJFuJ7LwIpEpOFmwGZZkRiB84PURz6U8=",
                TotalProfit = 0,
                TotalOrders = 0,
                TotalTradingDays = 0
            });

            // Seed a sample trading session
            modelBuilder.Entity<TradingSession>().HasData(new TradingSession
            {
                Id = 1,
                UserId = 5,
                Instrument = "S&P 500",
                TradingDay = "2024-01-01",
                CurrentBarIndex = 0,
                HasOpenOrder = false,
                EntryPrice = null,
                CurrentProfitLoss = null,
                TotalProfitLoss = 0,
                TotalOrders = 0
            });
        }
    }
}
