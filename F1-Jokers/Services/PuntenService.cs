using F1Jokers.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace F1Jokers.Services
{
    public class PuntenService
    {
        private readonly AppDbContext _context;

        public PuntenService(AppDbContext context)
        {
            _context = context;
        }

        public async Task BerekenPuntenVoorRaceAsync(string raceId)
        {
            // 1. Haal de uitslagen, voorspellingen en rekenregels op
            var uitslagen = await _context.Uitslagen.Where(u => u.RaceID == raceId).ToListAsync();
            var voorspellingen = await _context.Voorspellingen.Where(v => v.RaceID == raceId).ToListAsync();
            var regels = await _context.PuntenParameters.ToDictionaryAsync(p => p.Parameter, p => p.Waarde);

            if (!uitslagen.Any() || !voorspellingen.Any()) return;

            var top10StartNrs = uitslagen.Where(u => u.TypeResultaat.StartsWith("RacePos")).Select(u => u.StartNr).ToList();
            var top5SprintStartNrs = uitslagen.Where(u => u.TypeResultaat.StartsWith("SprintPos")).Select(u => u.StartNr).ToList();

            // 2. Bereken de punten per individuele voorspelling
            foreach (var voorspelling in voorspellingen)
            {
                voorspelling.BehaaldePunten = 0;

                bool isRacePos = voorspelling.TypeVoorspelling.StartsWith("RacePos");
                bool isSprintPos = voorspelling.TypeVoorspelling.StartsWith("SprintPos");

                // Basis punten (+3)
                if (isRacePos && top10StartNrs.Contains(voorspelling.StartNr))
                {
                    voorspelling.BehaaldePunten += regels["RacePosBijTop10"];
                }
                else if (isSprintPos && top5SprintStartNrs.Contains(voorspelling.StartNr))
                {
                    voorspelling.BehaaldePunten += regels["SprintPosBijTop5"];
                }

                // Exacte bonus punten
                var exacteUitslag = uitslagen.FirstOrDefault(u => u.TypeResultaat == voorspelling.TypeVoorspelling);

                if (exacteUitslag != null && exacteUitslag.StartNr == voorspelling.StartNr)
                {
                    switch (voorspelling.TypeVoorspelling)
                    {
                        case "RacePole": voorspelling.BehaaldePunten += regels["RacePole"]; break;
                        case "SnelsteRonde": voorspelling.BehaaldePunten += regels["SnelsteRonde"]; break;
                        case "RacePos1": voorspelling.BehaaldePunten += regels["RacePos1Juist"]; break;
                        case "RacePos2": voorspelling.BehaaldePunten += regels["RacePos2Juist"]; break;
                        case "RacePos3": voorspelling.BehaaldePunten += regels["RacePos3Juist"]; break;

                        case "SprintPole": voorspelling.BehaaldePunten += regels["SprintPole"]; break;
                        case "SprintPos1": voorspelling.BehaaldePunten += regels["SprintPos1Juist"]; break;
                        case "SprintPos2": voorspelling.BehaaldePunten += regels["SprintPos2Juist"]; break;
                        case "SprintPos3": voorspelling.BehaaldePunten += regels["SprintPos3Juist"]; break;
                        case "SprintPos4": voorspelling.BehaaldePunten += regels["SprintPos4Juist"]; break;
                        case "SprintPos5": voorspelling.BehaaldePunten += regels["SprintPos5Juist"]; break;

                        default:
                            if (isRacePos)
                            {
                                voorspelling.BehaaldePunten += regels["RacePos4-10Juist"];
                            }
                            break;
                    }
                }
            }

            // Sla de individuele scores eerst op
            await _context.SaveChangesAsync();

            // 3. TOTAALSCORE BEREKENING: Update het algemeen klassement
            var gebruikers = await _context.Gebruikers.ToListAsync();
            var alleVoorspellingen = await _context.Voorspellingen.ToListAsync();

            foreach (var gebruiker in gebruikers)
            {
                // Tel alle behaalde punten van deze specifieke gebruiker op
                int totaalScore = alleVoorspellingen
                    .Where(v => v.GebruikerID == gebruiker.GebruikerID)
                    .Sum(v => v.BehaaldePunten);

                // AANGEPAST: Gebruikt nu jouw eigen property GebruikerPoints
                gebruiker.GebruikerPoints = totaalScore;
            }

            // Sla de nieuwe totale standen op in de database
            await _context.SaveChangesAsync();
        }
    }
}