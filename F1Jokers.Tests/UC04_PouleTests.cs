using Xunit;
using F1Jokers.Controllers;
using F1Jokers.Data;
using F1Jokers.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace F1Jokers.Tests
{
    public class UC04_PouleTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        // UC-04: Controleert of de poule-tussenstand correct sorteert op behaalde punten (hoog naar laag)
        [Fact]
        public void Index_GenereertKlassement_GesorteerdOpAantalPunten()
        {
            var context = GetInMemoryDbContext();
            context.Kalender.Add(new Kalender { RaceID = "1", Racenaam = "AusGP", Datum = DateTime.Now.AddDays(-2), Deadline = DateTime.Now.AddDays(-3), Racetype = "Race" });

            // OPLOSSING: Email en Password toegevoegd omdat de database anders crasht op ontbrekende verplichte velden
            context.Gebruikers.Add(new Gebruiker
            {
                GebruikerID = 1,
                Username = "SlechteVoorspeller",
                Email = "slecht@test.nl",
                Password = "123",
                GebruikerPoints = 12,
                Rol = "Deelnemer"
            });
            context.Gebruikers.Add(new Gebruiker
            {
                GebruikerID = 2,
                Username = "GoedeVoorspeller",
                Email = "goed@test.nl",
                Password = "123",
                GebruikerPoints = 96,
                Rol = "Deelnemer"
            });
            context.SaveChanges();

            var controller = new PouleController(context);

            var result = controller.Index(null) as ViewResult;
            var model = result?.Model as PouleViewModel;

            Assert.NotNull(model);

            // Als het goed is staat degene met 96 punten nu bovenaan (First)
            Assert.Equal("GoedeVoorspeller", model.Ranglijst.First().Username);
            Assert.Equal("SlechteVoorspeller", model.Ranglijst.Last().Username);
        }
    }
}