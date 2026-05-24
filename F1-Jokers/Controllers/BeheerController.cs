using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using F1Jokers.Services;
using F1Jokers.Data;

namespace F1Jokers.Controllers
{
    [Authorize(Roles = "Beheerder")]
    public class BeheerController : Controller
    {
        // --- Fields ---
        private readonly F1ApiService _f1ApiService;
        private readonly PuntenService _puntenService;
        private readonly AppDbContext _context;

        // --- Constructor ---
        public BeheerController(F1ApiService f1ApiService, PuntenService puntenService, AppDbContext context)
        {
            _f1ApiService = f1ApiService;
            _puntenService = puntenService;
            _context = context;
        }

        // --- Actions ---
        public IActionResult Index()
        {
            // Haal de kalender op. We filteren "Seizoen" en Sprintraces ("S") eruit voor een overzichtelijke lijst.
            var kalender = _context.Kalender
                .Where(k => !k.RaceID.StartsWith("Seizoen") && !k.RaceID.EndsWith("S"))
                .OrderBy(k => k.Deadline)
                .ToList();

            // Geef de kalender door aan de View
            ViewBag.KalenderLijst = kalender;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HaalUitslagenOp(string raceId)
        {
            if (string.IsNullOrWhiteSpace(raceId))
            {
                TempData["ErrorMessage"] = "Selecteer a.u.b. een geldige race.";
                return RedirectToAction("Index");
            }

            try
            {
                // Geef het geselecteerde RaceID door aan je API service
                string lokaalRaceId = await _f1ApiService.HaalEnVerwerkRaceAsync(raceId);

                // Bereken de punten voor hoofdrace én sprintrace
                await _puntenService.BerekenPuntenVoorRaceAsync(lokaalRaceId);
                await _puntenService.BerekenPuntenVoorRaceAsync(lokaalRaceId + "S");

                TempData["SuccessMessage"] = $"De uitslagen, punten én flessen zijn suksesvol verwerkt voor Race ID: {lokaalRaceId}!";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = $"Er ging iets mis bij het verwerken: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        /* ----------------------- HIERONDER CODE VOOR HASHEN */
        [HttpGet]
        public async Task<IActionResult> MigreerWachtwoorden()
        {
            var gebruikers = _context.Gebruikers.ToList();
            int gehashteAccounts = 0;

            foreach (var g in gebruikers)
            {
                // Controleer of het wachtwoord nog niet gehasht is met BCrypt
                if (!string.IsNullOrEmpty(g.Password) &&
                    !g.Password.StartsWith("$2a$") &&
                    !g.Password.StartsWith("$2b$") &&
                    !g.Password.StartsWith("$2y$"))
                {
                    g.Password = BCrypt.Net.BCrypt.HashPassword(g.Password);
                    gehashteAccounts++;
                }
            }

            if (gehashteAccounts > 0)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{gehashteAccounts} gebruikerswachtwoorden zijn succesvol omgezet naar BCrypt-hashes!";
            }
            else
            {
                TempData["ErrorMessage"] = "Er hoefde niets te worden omgezet; alle wachtwoorden zijn al gehasht.";
            }

            return RedirectToAction("Index");
        }
    }
}