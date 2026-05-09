using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using F1Jokers.Data;
using F1Jokers.Models;
using Microsoft.Extensions.Logging;

namespace F1Jokers.Services
{
    public class VoorspellingKopieerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VoorspellingKopieerService> _logger;

        public VoorspellingKopieerService(IServiceProvider serviceProvider, ILogger<VoorspellingKopieerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🏁 VoorspellingKopieerService (met Afgelast-logica) is gestart...");

            while (!stoppingToken.IsCancellationRequested)
            {
                await VerwerkVerlopenDeadlinesAsync();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task VerwerkVerlopenDeadlinesAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. Zoek verlopen races die NIET zijn afgelast en nog NIET zijn verwerkt
                var verlopenRaces = await context.Kalender
                    .Where(k => k.Deadline < DateTime.Now
                             && !k.IsVerwerkt
                             && k.Racetype != "Seizoen"
                             && !k.IsAfgelast) // AANGEPAST: Negeer afgelaste races
                    .ToListAsync();

                foreach (var race in verlopenRaces)
                {
                    _logger.LogInformation($"⏰ Verwerken van deadline: {race.Racenaam}");

                    var alleGebruikers = await context.Gebruikers.ToListAsync();

                    foreach (var gebruiker in alleGebruikers)
                    {
                        bool heeftVoorspeld = await context.Voorspellingen
                            .AnyAsync(v => v.GebruikerID == gebruiker.GebruikerID && v.RaceID == race.RaceID);

                        if (!heeftVoorspeld)
                        {
                            // 2. SLIMME ZOEKTOCHT: Zoek de meest recente race van hetzelfde type die NIET is afgelast
                            var eerdereVoorspellingen = await (from v in context.Voorspellingen
                                                               join k in context.Kalender on v.RaceID equals k.RaceID
                                                               where v.GebruikerID == gebruiker.GebruikerID
                                                                  && k.Racetype == race.Racetype
                                                                  && k.Datum < race.Datum
                                                                  && !k.IsAfgelast // AANGEPAST: Spring over afgelaste races heen
                                                               select new { v, k })
                                                               .ToListAsync();

                            if (eerdereVoorspellingen.Any())
                            {
                                // Pak de ID van de LAATSTE race die echt is doorgegaan
                                var laatstBekendeRaceId = eerdereVoorspellingen
                                    .OrderByDescending(x => x.k.Datum)
                                    .First().k.RaceID;

                                var teKopierenData = eerdereVoorspellingen
                                    .Where(x => x.k.RaceID == laatstBekendeRaceId)
                                    .Select(x => x.v)
                                    .ToList();

                                foreach (var oud in teKopierenData)
                                {
                                    context.Voorspellingen.Add(new Voorspelling
                                    {
                                        GebruikerID = gebruiker.GebruikerID,
                                        RaceID = race.RaceID,
                                        TypeVoorspelling = oud.TypeVoorspelling,
                                        StartNr = oud.StartNr
                                    });
                                }
                                _logger.LogInformation($"✅ Data van '{laatstBekendeRaceId}' gekopieerd naar '{race.RaceID}' voor {gebruiker.Username}.");
                            }
                        }
                    }

                    race.IsVerwerkt = true;
                    await context.SaveChangesAsync();
                }

                // EXTRA: Markeer afgelaste races ook als 'verwerkt' zodat de robot ze niet blijft scannen
                var afgelasteRaces = await context.Kalender
                    .Where(k => k.Deadline < DateTime.Now && !k.IsVerwerkt && k.IsAfgelast)
                    .ToListAsync();

                foreach (var afg in afgelasteRaces)
                {
                    afg.IsVerwerkt = true;
                    _logger.LogInformation($"🚫 Race {afg.Racenaam} is afgelast. Gemarkeerd als verwerkt zonder kopieer-actie.");
                }
                if (afgelasteRaces.Any()) await context.SaveChangesAsync();
            }
        }
    }
}