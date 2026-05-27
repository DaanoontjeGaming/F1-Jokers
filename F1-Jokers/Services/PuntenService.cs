using F1Jokers.Data;
using F1Jokers.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            var top10StartNrs = uitslagen.Where(u => u.TypeResultaat.StartsWith("RacePos") && u.StartNr.HasValue).Select(u => u.StartNr.Value).ToList();
            var top5SprintStartNrs = uitslagen.Where(u => u.TypeResultaat.StartsWith("SprintPos") && u.StartNr.HasValue).Select(u => u.StartNr.Value).ToList();

            foreach (var voorspelling in voorspellingen)
            {
                voorspelling.BehaaldePunten = 0;

                bool isRacePos = voorspelling.TypeVoorspelling.StartsWith("RacePos");
                bool isSprintPos = voorspelling.TypeVoorspelling.StartsWith("SprintPos");

                if (isRacePos && voorspelling.StartNr.HasValue && top10StartNrs.Contains(voorspelling.StartNr.Value))
                {
                    voorspelling.BehaaldePunten += regels.GetValueOrDefault("RacePosBijTop10", 0);
                }
                else if (isSprintPos && voorspelling.StartNr.HasValue && top5SprintStartNrs.Contains(voorspelling.StartNr.Value))
                {
                    voorspelling.BehaaldePunten += regels.GetValueOrDefault("SprintPosBijTop5", 0);
                }

                var exacteUitslag = uitslagen.FirstOrDefault(u => u.TypeResultaat == voorspelling.TypeVoorspelling);

                if (exacteUitslag != null && exacteUitslag.StartNr == voorspelling.StartNr)
                {
                    switch (voorspelling.TypeVoorspelling)
                    {
                        case "RacePole": voorspelling.BehaaldePunten += regels.GetValueOrDefault("RacePole", 0); break;
                        case "SnelsteRonde": voorspelling.BehaaldePunten += regels.GetValueOrDefault("SnelsteRonde", 0); break;
                        case "RacePos1": voorspelling.BehaaldePunten += regels.GetValueOrDefault("RacePos1Juist", 0); break;
                        case "RacePos2": voorspelling.BehaaldePunten += regels.GetValueOrDefault("RacePos2Juist", 0); break;
                        case "RacePos3": voorspelling.BehaaldePunten += regels.GetValueOrDefault("RacePos3Juist", 0); break;

                        case "SprintPole": voorspelling.BehaaldePunten += regels.GetValueOrDefault("SprintPole", 0); break;
                        case "SprintPos1": voorspelling.BehaaldePunten += regels.GetValueOrDefault("SprintPos1Juist", 0); break;
                        case "SprintPos2": voorspelling.BehaaldePunten += regels.GetValueOrDefault("SprintPos2Juist", 0); break;
                        case "SprintPos3": voorspelling.BehaaldePunten += regels.GetValueOrDefault("SprintPos3Juist", 0); break;
                        case "SprintPos4": voorspelling.BehaaldePunten += regels.GetValueOrDefault("SprintPos4Juist", 0); break;
                        case "SprintPos5": voorspelling.BehaaldePunten += regels.GetValueOrDefault("SprintPos5Juist", 0); break;

                        default:
                            if (isRacePos) voorspelling.BehaaldePunten += regels.GetValueOrDefault("RacePos4-10Juist", 0);
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
                gebruiker.GebruikerPoints = alleVoorspellingen
                    .Where(v => v.GebruikerID == gebruiker.GebruikerID)
                    .Sum(v => v.BehaaldePunten);

                gebruiker.Champagne = 0;
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
            try
            {
                var wkCoureurs = await _context.WKStandCoureurs.OrderBy(c => c.Positie).ToListAsync();
                var wkTeams = await _context.WKStandTeams.OrderBy(t => t.Positie).ToListAsync();

                if (!wkCoureurs.Any() && !wkTeams.Any())
                {
                    throw new Exception("Geen actuele WK-standen gevonden in de database. Sla eerst de uitslagen van de laatste race op.");
                }

                var oudeUitslagen = await _context.Uitslagen.Where(u => u.RaceID == seizoenId).ToListAsync();
                if (oudeUitslagen.Any())
                {
                    _context.Uitslagen.RemoveRange(oudeUitslagen);
                    await _context.SaveChangesAsync();
                }

                var uitslagenDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var c in wkCoureurs)
                {
                    uitslagenDict[$"SeizoenCPos{c.Positie}"] = c.StartNr;
                }

                foreach (var t in wkTeams)
                {
                    uitslagenDict[$"SeizoenTPos{t.Positie}"] = t.TeamID;
                }

                if (wkCoureurs.Any())
                {
                    int maxWins = wkCoureurs.Max(c => c.Overwinningen);
                    var topDriver = wkCoureurs.FirstOrDefault(c => c.Overwinningen == maxWins);
                    if (topDriver != null)
                    {
                        uitslagenDict["SeizoenMeesteWinst"] = topDriver.StartNr;
                    }
                }

                foreach (var kvp in uitslagenDict)
                {
                    var nieuweUitslag = new Uitslag
                    {
                        RaceID = seizoenId,
                        TypeResultaat = kvp.Key
                    };

                    // HIER ZIT DE FIX: TeamID gaat netjes in de TeamId kolom!
                    if (kvp.Key.StartsWith("SeizoenTPos", StringComparison.OrdinalIgnoreCase))
                    {
                        nieuweUitslag.TeamId = kvp.Value;
                    }
                    else
                    {
                        nieuweUitslag.StartNr = kvp.Value;
                    }

                    _context.Uitslagen.Add(nieuweUitslag);
                }

                await _context.SaveChangesAsync();

                var verseUitslagen = await _context.Uitslagen.Where(u => u.RaceID == seizoenId).ToListAsync();
                var voorspellingen = await _context.Voorspellingen.Where(v => v.RaceID == seizoenId).ToListAsync();
                int puntenPerGoedeVoorspelling = 25;

                foreach (var voorspelling in voorspellingen)
                {
                    voorspelling.BehaaldePunten = 0;

                    var matchingUitslag = verseUitslagen.FirstOrDefault(u => u.TypeResultaat.Equals(voorspelling.TypeVoorspelling, StringComparison.OrdinalIgnoreCase));

                    if (matchingUitslag != null)
                    {
                        if (voorspelling.TypeVoorspelling.StartsWith("SeizoenTPos", StringComparison.OrdinalIgnoreCase))
                        {
                            // Nu vergelijken we netjes de TeamId's met elkaar!
                            if (voorspelling.TeamId == matchingUitslag.TeamId || voorspelling.StartNr == matchingUitslag.TeamId)
                            {
                                voorspelling.BehaaldePunten = puntenPerGoedeVoorspelling;
                            }
                        }
                        else
                        {
                            if (voorspelling.StartNr == matchingUitslag.StartNr)
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
                    gebruiker.GebruikerPoints = alleVoorspellingen
                        .Where(v => v.GebruikerID == gebruiker.GebruikerID)
                        .Sum(v => v.BehaaldePunten);
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                string echteFout = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                throw new Exception($"Database weigert opslaan: {echteFout}");
            }
        }
    }
}