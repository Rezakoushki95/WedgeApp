using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<FiveMinuteBar> FiveMinuteBars { get; set; }
        public DbSet<MarketDataDay> MarketDataDays { get; set; }
        public DbSet<MarketDataMonth> MarketDataMonths { get; set; }
        public DbSet<Journey> Journeys { get; set; }
        public DbSet<Trade> Trades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // MarketDataMonth to MarketDataDay (1-to-Many)
            modelBuilder.Entity<MarketDataMonth>()
                .HasMany(m => m.Days)
                .WithOne(d => d.MarketDataMonth)
                .HasForeignKey(d => d.MarketDataMonthId);

            // MarketDataDay to FiveMinuteBar (1-to-Many)
            modelBuilder.Entity<MarketDataDay>()
                .HasMany(d => d.FiveMinuteBars)
                .WithOne(fb => fb.MarketDataDay)
                .HasForeignKey(fb => fb.MarketDataDayId);
        }
    }
}
