using Microsoft.EntityFrameworkCore;
using F1Jokers.Models;

namespace F1Jokers.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Alle tabellen binnen de applicatie
        public DbSet<Gebruiker> Gebruikers { get; set; }
        public DbSet<Voorspelling> Voorspellingen { get; set; }
        public DbSet<Kalender> Kalender { get; set; }
        public DbSet<Coureur> Coureurs { get; set; }
        public DbSet<Team> Teams { get; set; }

        // De twee tabellen voor het uitslagen- en puntensysteem
        public DbSet<Uitslag> Uitslagen { get; set; }
        public DbSet<PuntenParameter> PuntenParameters { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Samengestelde sleutel voor de Voorspellingen tabel
            modelBuilder.Entity<Voorspelling>()
                .HasKey(v => new { v.GebruikerID, v.RaceID, v.TypeVoorspelling });

            // Samengestelde sleutel voor de Uitslagen tabel
            modelBuilder.Entity<Uitslag>()
                .HasKey(u => new { u.RaceID, u.TypeResultaat });
        }
    }
}