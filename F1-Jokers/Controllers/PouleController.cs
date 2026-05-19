using Microsoft.AspNetCore.Mvc;
using F1Jokers.Models;
using F1Jokers.Data; // Zorg dat dit klopt met jouw database namespace
using System.Linq;
using System.Collections.Generic;

namespace F1Jokers.Controllers
{
    public class PouleController : Controller
    {
        private readonly AppDbContext _context;

        public PouleController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 1. Haal de Deelnemers op, gesorteerd op punten (hoog naar laag)
            var deelnemers = _context.Gebruikers
                .Where(g => g.Rol == "Deelnemer")
                .OrderByDescending(g => g.GebruikerPoints)
                .ToList();

            var ranglijst = new List<GebruikerKlassementItem>();

            // Variabelen voor de "gedeelde plek" logica
            int huidigeIndex = 1;      // Telt gewoon op: 1, 2, 3, 4, 5
            int weergavePositie = 1;   // De rank die we op het scherm tonen
            int? vorigePunten = null;  // Onthoudt de score van de vorige iteratie

            // 2. Bouw het klassement op
            foreach (var speler in deelnemers)
            {
                int punten = speler.GebruikerPoints ?? 0;

                // Als dit NIET de eerste speler is, én hij heeft MINDER punten dan de vorige speler:
                // Dan updaten we de weergave positie naar de huidige index.
                // (Als de punten wél gelijk zijn, slaan we deze stap over en houden ze dezelfde weergavePositie!)
                if (vorigePunten.HasValue && punten < vorigePunten.Value)
                {
                    weergavePositie = huidigeIndex;
                }

                // Voeg de speler toe aan de lijst met de (eventueel gedeelde) weergave positie
                ranglijst.Add(new GebruikerKlassementItem
                {
                    Positie = weergavePositie,
                    Username = speler.Username,
                    TotalePunten = punten,
                    VerschilVorigGPWeekend = 0
                });

                // Sla de punten op voor de vergelijking in de volgende ronde
                vorigePunten = punten;

                // De interne teller gaat altijd +1, ongeacht een gelijkspel
                huidigeIndex++;
            }

            var model = new PouleViewModel
            {
                AlgemenePouleNaam = "F1-Jokers Algemeen Klassement",
                Ranglijst = ranglijst
            };

            return View(model);
        }
    }
}