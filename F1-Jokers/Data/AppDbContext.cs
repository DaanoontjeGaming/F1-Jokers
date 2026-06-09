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
        public DbSet<Team> Teams { get; set; }
        public DbSet<Coureur> Coureurs { get; set; }
        public DbSet<Kalender> Kalender { get; set; }
        public DbSet<Voorspelling> Voorspellingen { get; set; }
        public DbSet<Uitslag> Uitslagen { get; set; }
        public DbSet<PuntenParameter> PuntenParameters { get; set; }
        public DbSet<WKStandCoureur> WKStandCoureurs { get; set; }
        public DbSet<WKStandTeam> WKStandTeams { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Voorspelling>()
                .HasKey(v => new { v.GebruikerID, v.RaceID, v.TypeVoorspelling });

            modelBuilder.Entity<Uitslag>()
                .HasKey(u => new { u.RaceID, u.TypeResultaat });

            modelBuilder.Entity<PuntenParameter>()
                .ToTable("Puntenberekening Poule")
                .HasKey(p => p.PuntenID);

            modelBuilder.Entity<WKStandCoureur>()
                .ToTable("WKStandCoureurs")
                .HasKey(w => w.StartNr);

            modelBuilder.Entity<WKStandTeam>()
                .ToTable("WKStandTeams")
                .HasKey(w => w.TeamID);
        }
    }
}