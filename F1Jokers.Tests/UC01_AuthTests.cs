using Xunit;
using F1Jokers.Controllers;
using F1Jokers.Data;
using F1Jokers.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System;

namespace F1Jokers.Tests
{
    public class UC01_AuthTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        // UC-01: Controleert of een geldige inlogpoging via de API succesvol autoriseert
        [Fact]
        public void Inloggen_GeldigeGegevens_GeeftOkResultaat()
        {
            var context = GetInMemoryDbContext();
            context.Gebruikers.Add(new Gebruiker { GebruikerID = 1, Email = "test@test.nl", Password = "ValidPassword123", Username = "Tester", Rol = "Deelnemer" });
            context.SaveChanges();
            var controller = new AuthController(context);
            var request = new LoginRequest { Email = "test@test.nl", Password = "ValidPassword123" };

            var result = controller.Inloggen(request);

            Assert.IsType<OkObjectResult>(result);
        }

        // UC-01: Controleert of een inlogpoging met een foutief wachtwoord direct wordt geweigerd
        [Fact]
        public void Inloggen_OnjuistWachtwoord_GeeftUnauthorized()
        {
            var context = GetInMemoryDbContext();

            // OPLOSSING: Username en Rol toegevoegd aan de testdata!
            context.Gebruikers.Add(new Gebruiker
            {
                GebruikerID = 2,
                Email = "test2@test.nl",
                Password = "ValidPassword123",
                Username = "TestUser2",
                Rol = "Deelnemer"
            });
            context.SaveChanges();

            var controller = new AuthController(context);
            var request = new LoginRequest { Email = "test2@test.nl", Password = "WrongPassword" };

            var result = controller.Inloggen(request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }
    }
}