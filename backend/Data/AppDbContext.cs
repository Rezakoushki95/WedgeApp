using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<FiveMinuteBar> FiveMinuteBars { get; set; }
        public DbSet<TradingSession> TradingSessions { get; set; }
        public DbSet<Day> Days { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define relationships
            modelBuilder.Entity<TradingSession>()
                .HasMany(ts => ts.Days)
                .WithOne(d => d.TradingSession)
                .HasForeignKey(d => d.TradingSessionId);

        }
    }
}
