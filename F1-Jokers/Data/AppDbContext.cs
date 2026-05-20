using Microsoft.EntityFrameworkCore;
using F1Jokers.Models;

namespace F1Jokers.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<Gebruiker> Gebruikers { get; set; }
        public DbSet<Voorspelling> Voorspellingen { get; set; }
        public DbSet<Kalender> Kalender { get; set; }
        public DbSet<Coureur> Coureurs { get; set; }
        public DbSet<Team> Teams { get; set; }

        public DbSet<Uitslag> Uitslagen { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- JOUW BESTAANDE CONFIGURATIES (LAAT DEZE STAAN!) ---
            // Bijvoorbeeld de samengestelde sleutel voor Voorspellingen die je waarschijnlijk al had:
            modelBuilder.Entity<Voorspelling>()
                .HasKey(v => new { v.GebruikerID, v.RaceID, v.TypeVoorspelling });


            // --- 2. VOEG ALLEEN DEZE NIEUWE CONFIGURATIE TOE VOOR UITSLAGEN ---
            modelBuilder.Entity<Uitslag>()
                .HasKey(u => new { u.RaceID, u.TypeResultaat });
        }
    }
}