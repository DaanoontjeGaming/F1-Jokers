using Xunit;
using F1Jokers.Services;
using F1Jokers.Data;
using F1Jokers.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace F1Jokers.Tests
{
    public class UC05_KopieerServiceTests
    {
        private DbContextOptions<AppDbContext> GetInMemoryOptions()
        {
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        // UC-05: Controleert of de achtergrondservice ontbrekende voorspellingen kopieert van een eerdere geldige race
        [Fact]
        public async Task Service_KopieertLaatsteVoorspelling_WanneerDeelnemerDeadlineMist()
        {
            var options = GetInMemoryOptions();
            using (var context = new AppDbContext(options))
            {
                context.Kalender.Add(new Kalender { RaceID = "1", Racenaam = "Race 1", Datum = DateTime.Now.AddDays(-5), Deadline = DateTime.Now.AddDays(-6), Racetype = "Race", IsVerwerkt = true });
                context.Kalender.Add(new Kalender { RaceID = "2", Racenaam = "Race 2", Datum = DateTime.Now.AddDays(-1), Deadline = DateTime.Now.AddDays(-2), Racetype = "Race", IsVerwerkt = false });

                context.Gebruikers.Add(new Gebruiker
                {
                    GebruikerID = 10,
                    Username = "VergetelDeelnemer",
                    Email = "vergeten@test.nl",
                    Password = "123",
                    Rol = "Deelnemer"
                });

                context.Voorspellingen.Add(new Voorspelling { GebruikerID = 10, RaceID = "1", TypeVoorspelling = "RacePos1", StartNr = 3 });
                await context.SaveChangesAsync();
            }

            var serviceProviderMock = new Mock<IServiceProvider>();
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

            serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactoryMock.Object);
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);
            serviceScopeMock.Setup(x => x.ServiceProvider.GetService(typeof(AppDbContext))).Returns(new AppDbContext(options));

            var loggerMock = new Mock<ILogger<VoorspellingKopieerService>>();
            var service = new VoorspellingKopieerService(serviceProviderMock.Object, loggerMock.Object);

            var cancellationToken = new CancellationTokenSource();

            await service.StartAsync(cancellationToken.Token);


            await Task.Delay(500);


            await service.StopAsync(cancellationToken.Token);

            using (var context = new AppDbContext(options))
            {
                var gekopieerdeVoorspelling = context.Voorspellingen.FirstOrDefault(v => v.RaceID == "2" && v.GebruikerID == 10);

                Assert.NotNull(gekopieerdeVoorspelling);
                Assert.Equal(3, gekopieerdeVoorspelling.StartNr);
            }
        }
    }
}