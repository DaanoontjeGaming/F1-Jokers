using Xunit;
using F1Jokers.Controllers;
using F1Jokers.Data;
using F1Jokers.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System;
using System.Linq;

namespace F1Jokers.Tests
{
    public class UC02_UC03_VoorspellingTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        // UC-02 & UC-03: Controleert of de controller invoer correct verwerkt, oude records wist en nieuwe opslaat
        [Fact]
        public void Opslaan_GeldigeVoorspelling_VerwerktEnOverschrijftDatabaseRecords()
        {
            var context = GetInMemoryDbContext();
            context.Kalender.Add(new Kalender { RaceID = "1", Racenaam = "Australian GP", Datum = DateTime.Now.AddDays(2), Deadline = DateTime.Now.AddDays(1), Racetype = "Race" });
            context.SaveChanges();

            var controller = new VoorspellingController(context);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "1") }, "mock"));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            var dto = new VoorspellingSubmissionDto
            {
                RaceId = "1",
                RaceTop10 = "1,81,16,63,14,55,10,30,87,27"
            };

            var result = controller.Opslaan(dto);
            var opgeslagenRecords = context.Voorspellingen.Where(v => v.GebruikerID == 1 && v.RaceID == "1").ToList();

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(10, opgeslagenRecords.Count);
            Assert.Equal(1, opgeslagenRecords.First(v => v.TypeVoorspelling == "RacePos1").StartNr);
        }
    }
}