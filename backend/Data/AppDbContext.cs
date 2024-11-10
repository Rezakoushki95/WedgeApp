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
        public DbSet<MarketDataDay> MarketDataDays { get; set; }
        public DbSet<MarketDataMonth> MarketDataMonths { get; set; }
        public DbSet<AccessedDay> AccessedDays { get; set; }
        public DbSet<AccessedMonth> AccessedMonths { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User to TradingSession (1-to-1 relationship)
            modelBuilder.Entity<User>()
                .HasOne(u => u.CurrentTradingSession)
                .WithOne(ts => ts.User)
                .HasForeignKey<TradingSession>(ts => ts.UserId);

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

            // AccessedDay to User (Many-to-1)
            modelBuilder.Entity<AccessedDay>()
                .HasOne(ad => ad.User)
                .WithMany(u => u.AccessedDays)
                .HasForeignKey(ad => ad.UserId);

            // AccessedDay to MarketDataDay (Many-to-1)
            modelBuilder.Entity<AccessedDay>()
                .HasOne(ad => ad.MarketDataDay)
                .WithMany(d => d.AccessedDays)
                .HasForeignKey(ad => ad.MarketDataDayId);

            // AccessedMonth to User (Many-to-1)
            modelBuilder.Entity<AccessedMonth>()
                .HasOne(am => am.User)
                .WithMany(u => u.AccessedMonths)
                .HasForeignKey(am => am.UserId);

            // AccessedMonth to MarketDataMonth (Many-to-1)
            modelBuilder.Entity<AccessedMonth>()
                .HasOne(am => am.MarketDataMonth)
                .WithMany(m => m.AccessedMonths)
                .HasForeignKey(am => am.MarketDataMonthId);
        }
    }
}
