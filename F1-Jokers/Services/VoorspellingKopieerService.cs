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
            _logger.LogInformation("🏁 VoorspellingKopieerService is gestart en luistert op de achtergrond...");

            // Dit is de oneindige loop die elke minuut draait
            while (!stoppingToken.IsCancellationRequested)
            {
                await VerwerkVerlopenDeadlinesAsync();

                // Wacht 1 minuut voordat hij weer kijkt
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task VerwerkVerlopenDeadlinesAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. Zoek alle races (geen seizoensitems) waarvan de deadline net is verstreken én die nog niet verwerkt zijn
                var verlopenRaces = await context.Kalender
                    .Where(k => k.Deadline < DateTime.Now && !k.IsVerwerkt && k.Racetype != "Seizoen")
                    .ToListAsync();

                foreach (var race in verlopenRaces)
                {
                    _logger.LogInformation($"⏰ Deadline gepasseerd voor: {race.Racenaam}. Systeem start met controleren...");

                    var alleGebruikers = await context.Gebruikers.ToListAsync();

                    foreach (var gebruiker in alleGebruikers)
                    {
                        // 2. Check of de gebruiker zelf al een voorspelling heeft gedaan voor deze specifieke race
                        bool heeftVoorspeld = await context.Voorspellingen
                            .AnyAsync(v => v.GebruikerID == gebruiker.GebruikerID && v.RaceID == race.RaceID);

                        if (!heeftVoorspeld)
                        {
                            // 3. SLIMME ZOEKTOCHT: Zoek alle oude data van deze gebruiker voor ditzelfde racetype (bijv 'Race')
                            var eerdereVoorspellingen = await (from v in context.Voorspellingen
                                                               join k in context.Kalender on v.RaceID equals k.RaceID
                                                               where v.GebruikerID == gebruiker.GebruikerID
                                                                  && k.Racetype == race.Racetype
                                                                  && k.Datum < race.Datum
                                                               select new { v, k })
                                                               .ToListAsync();

                            if (eerdereVoorspellingen.Any())
                            {
                                // Sorteer op datum en pak de ID van de LAATSTE race waar hij echt data voor had
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
                                _logger.LogInformation($"✅ Voorspellingen van race '{laatstBekendeRaceId}' succesvol gekopieerd voor {gebruiker.Username} naar '{race.RaceID}'.");
                            }
                            else
                            {
                                _logger.LogWarning($"❌ Gebruiker {gebruiker.Username} had niets ingevuld voor {race.Racenaam} én we konden geen enkele eerdere race vinden om te kopiëren. 0 punten!");
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"👌 Gebruiker {gebruiker.Username} had netjes op tijd voorspeld voor {race.Racenaam}. Geen actie nodig.");
                        }
                    }

                    // 4. Markeer deze race als 'verwerkt' in de database
                    race.IsVerwerkt = true;
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"🏁 Verwerking voor {race.Racenaam} volledig afgerond en afgesloten!");
                }
            }
        }
    }
}