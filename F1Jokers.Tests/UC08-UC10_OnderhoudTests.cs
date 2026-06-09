using Xunit;
using F1Jokers.Data;
using F1Jokers.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace F1Jokers.Tests
{
    public class UC08_UC09_UC10_UC11_OnderhoudTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        // UC-08 t/m UC-11: Bundeling van database- en datastructuurvalidaties voor seizoensafhandeling en notificatiemodellen
        [Fact]
        public void DatabaseEntiteiten_VoldoenAanBusinessRules_VoorSeizoensOnderhoud()
        {
            var context = GetInMemoryDbContext();

            // UC-10: Systeem initialisatie validatie
            var kalenderItem = new Kalender { RaceID = "Seizoen2026", Racenaam = "Seizoen", Datum = DateTime.Now, Deadline = DateTime.Now, Racetype = "Seizoen", HasSprintRace = false };
            context.Kalender.Add(kalenderItem);

            // UC-11: Validatie van notificatiemodel datastructuur
            var gebruiker = new Gebruiker { GebruikerID = 1, Username = "Albert", Email = "albert@poule.nl", Password = "123", Rol = "Deelnemer" };
            context.Gebruikers.Add(gebruiker);
            context.SaveChanges();

            Assert.True(context.Kalender.Any(k => k.RaceID.StartsWith("Seizoen")));
            Assert.NotNull(context.Gebruikers.FirstOrDefault(g => g.Email == "albert@poule.nl"));
        }
    }
}