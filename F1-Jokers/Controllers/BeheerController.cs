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
        private readonly F1ApiService _f1ApiService;
        private readonly PuntenService _puntenService;
        private readonly AppDbContext _context;

        public BeheerController(F1ApiService f1ApiService, PuntenService puntenService, AppDbContext context)
        {
            _f1ApiService = f1ApiService;
            _puntenService = puntenService;
            _context = context;
        }

        public IActionResult Index()
        {
            var kalender = _context.Kalender
                .Where(k => !k.RaceID.StartsWith("Seizoen") && !k.RaceID.EndsWith("S"))
                .OrderBy(k => k.Deadline)
                .ToList();

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
                string lokaalRaceId = await _f1ApiService.HaalEnVerwerkRaceAsync(raceId);

                await _puntenService.BerekenPuntenVoorRaceAsync(lokaalRaceId);
                await _puntenService.BerekenPuntenVoorRaceAsync(lokaalRaceId + "S");

                TempData["SuccessMessage"] = $"De uitslagen, punten én flessen zijn succesvol verwerkt voor Race ID: {lokaalRaceId}!";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = $"Er ging iets mis bij het verwerken: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> MigreerWachtwoorden()
        {
            var gebruikers = _context.Gebruikers.ToList();
            int gehashteAccounts = 0;

            foreach (var g in gebruikers)
            {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BerekenSeizoenBonus(string seizoenId)
        {
            if (string.IsNullOrWhiteSpace(seizoenId))
            {
                TempData["ErrorMessage"] = "Selecteer of vul een geldig seizoens-ID in.";
                return RedirectToAction("Index");
            }

            try
            {
                await _puntenService.BerekenSeizoenBonusPuntenAsync(seizoenId);
                TempData["SuccessMessage"] = $"De seizoens-bonuspunten voor {seizoenId} zijn succesvol berekend en de ranglijst is bijgewerkt!";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = $"Fout bij berekenen bonuspunten: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}