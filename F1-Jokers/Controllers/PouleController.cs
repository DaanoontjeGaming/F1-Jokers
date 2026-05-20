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
        private readonly AppDbContext _context;

        public PouleController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string raceId)
        {
            // 1. Haal alle races op voor de dropdown (we filteren de 'Seizoen' records eruit)
            var kalender = _context.Kalender
                .Where(k => !k.RaceID.StartsWith("Seizoen"))
                .OrderBy(k => k.Deadline) // Netjes op volgorde van de kalenderdatum
                .ToList();

            // 2. Fallback-logica: Als er geen raceId is meegegeven, pakken we de meest recent gereden race
            if (string.IsNullOrEmpty(raceId) && kalender.Any())
            {
                var meestRecenteGeslotenRace = kalender
                    .Where(k => DateTime.Now > k.Deadline)
                    .OrderByDescending(k => k.Deadline)
                    .FirstOrDefault();

                // Als er nog geen enkele race gesloten is, pakken we gewoon de eerste race van het seizoen
                raceId = meestRecenteGeslotenRace?.RaceID ?? kalender.First().RaceID;
            }

            // 3. Haal de Deelnemers op uit de database
            // ARCHITECTUUR TIP VOOR JE SHOWCASE:
            // Nu pakken we nog de algemene 'GebruikerPoints'. Zodra je een tabel hebt die de scores 
            // PER RACE bijhoudt (bijv. UserScoresPerRace), kun je hier de query aanpassen naar:
            // .Where(g => g.Rol == "Deelnemer").Select(g => nieuwe berekening tot en met raceId)
            var deelnemers = _context.Gebruikers
                .Where(g => g.Rol == "Deelnemer")
                .OrderByDescending(g => g.GebruikerPoints)
                .ToList();

            var ranglijst = new List<GebruikerKlassementItem>();
            int huidigeIndex = 1;
            int weergavePositie = 1;
            int? vorigePunten = null;

            foreach (var speler in deelnemers)
            {
                int punten = speler.GebruikerPoints ?? 0;

                if (vorigePunten.HasValue && punten < vorigePunten.Value)
                {
                    weergavePositie = huidigeIndex;
                }

                ranglijst.Add(new GebruikerKlassementItem
                {
                    Positie = weergavePositie,
                    Username = speler.Username,
                    TotalePunten = punten,
                    VerschilVorigGPWeekend = 0,
                    AantalChampagneFlessen = 2
                });

                vorigePunten = punten;
                huidigeIndex++;
            }

            // 4. Bouw het complete model op inclusief geselecteerde data
            var model = new PouleViewModel
            {
                AlgemenePouleNaam = "F1-Jokers Algemeen Klassement",
                Ranglijst = ranglijst,
                SelectedRaceId = raceId,
                VolledigeKalender = kalender
            };

            return View(model);
        }
    }
}