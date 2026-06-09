using Xunit;
using F1Jokers.Services;
using F1Jokers.Data;
using F1Jokers.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace F1Jokers.Tests
{
    public class UC07_PuntenServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        // UC-07: Controleert de werking van de berekenings-engine inclusief juiste toekenning van bonuspunten parameters
        [Fact]
        public async Task BerekenPuntenVoorRaceAsync_VerwerktTotaalEnBonusScores_VolgensRegels()
        {
            var context = GetInMemoryDbContext();
            context.PuntenParameters.Add(new PuntenParameter { Parameter = "RacePosBijTop10", Waarde = 3 });
            context.PuntenParameters.Add(new PuntenParameter { Parameter = "RacePos1Juist", Waarde = 10 });

            // OPLOSSING: Verplichte velden (Email, Password, Rol, Username) toegevoegd aan de test-gebruiker!
            context.Gebruikers.Add(new Gebruiker
            {
                GebruikerID = 5,
                Username = "Kampioen",
                Email = "kampioen@f1.nl",
                Password = "123",
                Rol = "Deelnemer",
                GebruikerPoints = 0
            });

            context.Uitslagen.Add(new Uitslag { RaceID = "1", TypeResultaat = "RacePos1", StartNr = 3 }); // Hamilton wint
            context.Voorspellingen.Add(new Voorspelling { GebruikerID = 5, RaceID = "1", TypeVoorspelling = "RacePos1", StartNr = 3, BehaaldePunten = 0 });
            context.SaveChanges();

            var service = new PuntenService(context);
            await service.BerekenPuntenVoorRaceAsync("1");

            var geupdateGebruiker = context.Gebruikers.First(g => g.GebruikerID == 5);
            var geupdateVoorspelling = context.Voorspellingen.First(v => v.GebruikerID == 5 && v.RaceID == "1");

            Assert.Equal(13, geupdateVoorspelling.BehaaldePunten); // 3 (top10) + 10 (exact juist)
            Assert.Equal(13, geupdateGebruiker.GebruikerPoints);
        }
    }
}