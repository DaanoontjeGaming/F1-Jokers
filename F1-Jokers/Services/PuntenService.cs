using F1Jokers.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

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
            var uitslagen = await _context.Uitslagen.Where(u => u.RaceID == raceId).ToListAsync();
            var voorspellingen = await _context.Voorspellingen.Where(v => v.RaceID == raceId).ToListAsync();
            var regels = await _context.PuntenParameters.ToDictionaryAsync(p => p.Parameter, p => p.Waarde);

            if (!uitslagen.Any() || !voorspellingen.Any()) return;

            var top10StartNrs = uitslagen.Where(u => u.TypeResultaat.StartsWith("RacePos")).Select(u => u.StartNr).ToList();
            var top5SprintStartNrs = uitslagen.Where(u => u.TypeResultaat.StartsWith("SprintPos")).Select(u => u.StartNr).ToList();

            foreach (var voorspelling in voorspellingen)
            {
                voorspelling.BehaaldePunten = 0;

                bool isRacePos = voorspelling.TypeVoorspelling.StartsWith("RacePos");
                bool isSprintPos = voorspelling.TypeVoorspelling.StartsWith("SprintPos");

                if (isRacePos && voorspelling.StartNr.HasValue && top10StartNrs.Contains(voorspelling.StartNr.Value))
                {
                    voorspelling.BehaaldePunten += regels["RacePosBijTop10"];
                }
                else if (isSprintPos && voorspelling.StartNr.HasValue && top5SprintStartNrs.Contains(voorspelling.StartNr.Value))
                {
                    voorspelling.BehaaldePunten += regels["SprintPosBijTop5"];
                }

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
                            if (isRacePos) voorspelling.BehaaldePunten += regels["RacePos4-10Juist"];
                            break;
                    }
                }
            }

            await _context.SaveChangesAsync();

            var gebruikers = await _context.Gebruikers.ToListAsync();
            var alleVoorspellingen = await _context.Voorspellingen.ToListAsync();
            var alleUitslagen = await _context.Uitslagen.ToListAsync();

            foreach (var gebruiker in gebruikers)
            {
                int totaalScore = alleVoorspellingen
                    .Where(v => v.GebruikerID == gebruiker.GebruikerID)
                    .Sum(v => v.BehaaldePunten);

                gebruiker.GebruikerPoints = totaalScore;
            }

            foreach (var g in gebruikers)
            {
                g.Champagne = 0;
            }

            var verredenWeekenden = alleUitslagen
                .Select(u => u.RaceID.EndsWith("S") ? u.RaceID.Substring(0, u.RaceID.Length - 1) : u.RaceID)
                .Distinct()
                .ToList();

            foreach (var weekendId in verredenWeekenden)
            {
                var voorspellingenVanWeekend = alleVoorspellingen.Where(v => v.RaceID == weekendId || v.RaceID == weekendId + "S").ToList();

                var prestatiesPerGebruiker = voorspellingenVanWeekend
                    .GroupBy(v => v.GebruikerID)
                    .Select(group => new
                    {
                        GebruikerID = group.Key,
                        PuntenDitWeekend = group.Sum(v => v.BehaaldePunten)
                    })
                    .ToList();

                if (!prestatiesPerGebruiker.Any()) continue;

                int maxPunten = prestatiesPerGebruiker.Max(j => j.PuntenDitWeekend);

                if (maxPunten > 0)
                {
                    var topScorers = prestatiesPerGebruiker.Where(j => j.PuntenDitWeekend == maxPunten).ToList();

                    foreach (var winnaar in topScorers)
                    {
                        var u = gebruikers.FirstOrDefault(usr => usr.GebruikerID == winnaar.GebruikerID);
                        if (u != null)
                        {
                            u.Champagne++;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task BerekenSeizoenBonusPuntenAsync(string seizoenId)
        {
            var uitslagen = await _context.Uitslagen.Where(u => u.RaceID == seizoenId).ToListAsync();
            var voorspellingen = await _context.Voorspellingen.Where(v => v.RaceID == seizoenId).ToListAsync();

            if (!uitslagen.Any())
            {
                throw new System.Exception("Geen officiële eindstanden gevonden in de database voor dit seizoen. Voer eerst de uitslag in.");
            }

            if (!voorspellingen.Any())
            {
                throw new System.Exception("Geen seizoensvoorspellingen gevonden om te berekenen.");
            }

            int puntenPerGoedeVoorspelling = 25;

            foreach (var voorspelling in voorspellingen)
            {
                var exacteUitslag = uitslagen.FirstOrDefault(u => u.TypeResultaat == voorspelling.TypeVoorspelling);

                if (exacteUitslag != null)
                {
                    if (voorspelling.TypeVoorspelling.Contains("TPos"))
                    {
                        if (exacteUitslag.StartNr == voorspelling.TeamId)
                        {
                            voorspelling.BehaaldePunten = puntenPerGoedeVoorspelling;
                        }
                    }
                    else
                    {
                        if (exacteUitslag.StartNr == voorspelling.StartNr)
                        {
                            voorspelling.BehaaldePunten = puntenPerGoedeVoorspelling;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            var gebruikers = await _context.Gebruikers.ToListAsync();
            var alleVoorspellingen = await _context.Voorspellingen.ToListAsync();

            foreach (var gebruiker in gebruikers)
            {
                int totaalScore = alleVoorspellingen
                    .Where(v => v.GebruikerID == gebruiker.GebruikerID)
                    .Sum(v => v.BehaaldePunten);

                gebruiker.GebruikerPoints = totaalScore;
            }

            await _context.SaveChangesAsync();
        }
    }
}