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



    }


}

