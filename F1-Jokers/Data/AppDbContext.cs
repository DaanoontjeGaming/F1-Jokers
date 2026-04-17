using Microsoft.EntityFrameworkCore;
using F1Jokers.Models; 

namespace F1Jokers.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Jouw Datalaag: Koppelt de C# class 'Team' aan de MySQL tabel 'Teams'
        public DbSet<Team> Teams { get; set; }
    }
}