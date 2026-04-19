using Microsoft.EntityFrameworkCore;
using F1Jokers.Models;

namespace F1Jokers.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Team> Teams { get; set; }
        public DbSet<Coureur> Coureurs{ get; set; }
        public DbSet<Gebruiker> Gebruikers { get; set; }
        public DbSet<Kalender> Kalender { get; set; }
    }
}