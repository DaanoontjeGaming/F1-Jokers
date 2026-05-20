using Microsoft.AspNetCore.Mvc;
using F1Jokers.Models;
using F1Jokers.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace F1Jokers.Controllers
{
    [Authorize]
    public class PouleController : Controller
    {
        // --- Fields ---
        private readonly AppDbContext _context;

        // --- Constructor ---
        public PouleController(AppDbContext context)
        {
            _context = context;
        }

        // --- Actions ---
        public IActionResult Index(string raceId)
        {
            // --- AANGEPAST: Filter Seizoen-records én Sprintraces (eindigend op "S") eruit ---
            var kalender = _context.Kalender
                .Where(k => !k.RaceID.StartsWith("Seizoen") && !k.RaceID.EndsWith("S"))
                .OrderBy(k => k.Deadline)
                .ToList();

            if (string.IsNullOrEmpty(raceId) && kalender.Any())
            {
                var meestRecenteGeslotenRace = kalender
                    .Where(k => DateTime.Now > k.Deadline)
                    .OrderByDescending(k => k.Deadline)
                    .FirstOrDefault();

                raceId = meestRecenteGeslotenRace?.RaceID ?? kalender.First().RaceID;
            }

            var deelnemers = _context.Gebruikers
                .Where(g => g.Rol == "Deelnemer")
                .OrderByDescending(g => g.GebruikerPoints)
                .ToList();

            // De punten van de hoofdrace én de sprintrace worden hier al netjes bij elkaar opgeteld
            var voorspellingenDezeRace = _context.Voorspellingen
                .Where(v => v.RaceID == raceId || v.RaceID == raceId + "S")
                .ToList();

            var puntenDezeRace = voorspellingenDezeRace
                .GroupBy(v => v.GebruikerID)
                .ToDictionary(g => g.Key, g => g.Sum(v => v.BehaaldePunten));

            var oudeStand = deelnemers
                .Select(d => new {
                    d.GebruikerID,
                    OudePunten = (d.GebruikerPoints ?? 0) - (puntenDezeRace.ContainsKey(d.GebruikerID) ? puntenDezeRace[d.GebruikerID] : 0)
                })
                .OrderByDescending(x => x.OudePunten)
                .ToList();

            var vorigePosities = new Dictionary<int, int>();
            int oudePositie = 1;
            int? vorigeOudePunten = null;
            int oudeIndex = 1;

            foreach (var item in oudeStand)
            {
                if (vorigeOudePunten.HasValue && item.OudePunten < vorigeOudePunten.Value)
                {
                    oudePositie = oudeIndex;
                }
                vorigePosities[item.GebruikerID] = oudePositie;
                vorigeOudePunten = item.OudePunten;
                oudeIndex++;
            }

            var ranglijst = new List<GebruikerKlassementItem>();
            int huidigeIndex = 1;
            int weergavePositie = 1;
            int? vorigePunten = null;

            foreach (var speler in deelnemers)
            {
                int punten = speler.GebruikerPoints ?? 0;
                int racePunten = puntenDezeRace.ContainsKey(speler.GebruikerID) ? puntenDezeRace[speler.GebruikerID] : 0;

                if (vorigePunten.HasValue && punten < vorigePunten.Value)
                {
                    weergavePositie = huidigeIndex;
                }

                int positieVerschil = vorigePosities[speler.GebruikerID] - weergavePositie;

                ranglijst.Add(new GebruikerKlassementItem
                {
                    Positie = weergavePositie,
                    Username = speler.Username,
                    TotalePunten = punten,
                    VerschilVorigGPWeekend = positieVerschil,
                    PuntenGeselecteerdeGP = racePunten,
                    AantalChampagneFlessen = speler.Champagne
                });

                vorigePunten = punten;
                huidigeIndex++;
            }

            var model = new PouleViewModel
            {
                AlgemenePouleNaam = "F1-Jokers Poule Tussenstand",
                Ranglijst = ranglijst,
                SelectedRaceId = raceId,
                VolledigeKalender = kalender
            };

            return View(model);
        }
    }
}