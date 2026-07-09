using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using F1Jokers.Services;
using F1Jokers.Data;
using F1Jokers.Models;
using Microsoft.EntityFrameworkCore;

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
            // --- AANGEPAST: Robuuste sortering op het officiële numerieke RaceID ---
            var kalender = _context.Kalender
                .Where(k => !k.RaceID.StartsWith("Seizoen") && !k.RaceID.EndsWith("S"))
                .ToList() // Haal eerst op naar het geheugen
                .OrderBy(k => int.TryParse(k.RaceID, out int id) ? id : 9999) // Sorteer numeriek
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

        [HttpGet]
        public async Task<IActionResult> ExporteerEindstandCsv()
        {
            var gebruikers = await _context.Gebruikers.ToDictionaryAsync(g => g.GebruikerID);
            var kalender = await _context.Kalender.ToDictionaryAsync(k => k.RaceID);
            var uitslagen = await _context.Uitslagen.ToListAsync();
            var voorspellingen = await _context.Voorspellingen.OrderBy(v => v.GebruikerID).ThenBy(v => v.RaceID).ToListAsync();

            var csv = new System.Text.StringBuilder();

            csv.AppendLine("sep=;");
            csv.AppendLine("Username;Totale Punten;Race ID;Racenaam;Type Voorspelling;Voorspeld StartNr;Voorspeld TeamID;Werkelijke Uitslag StartNr;Werkelijke Uitslag TeamID;Behaalde Punten op Voorspelling");

            foreach (var v in voorspellingen)
            {
                var gebruiker = gebruikers.GetValueOrDefault(v.GebruikerID);
                var race = kalender.GetValueOrDefault(v.RaceID);
                var uitslag = uitslagen.FirstOrDefault(u => u.RaceID == v.RaceID && u.TypeResultaat == v.TypeVoorspelling);

                var username = gebruiker?.Username ?? "Onbekend";
                var totalePunten = gebruiker?.GebruikerPoints ?? 0;
                var raceNaam = race?.Racenaam ?? "Onbekend";

                var voorspeldStartNr = v.StartNr?.ToString() ?? "";
                var voorspeldTeamId = v.TeamId?.ToString() ?? "";

                var uitslagStartNr = uitslag?.StartNr?.ToString() ?? "";
                var uitslagTeamId = uitslag?.TeamId?.ToString() ?? "";

                csv.AppendLine($"{username};{totalePunten};{v.RaceID};{raceNaam};{v.TypeVoorspelling};{voorspeldStartNr};{voorspeldTeamId};{uitslagStartNr};{uitslagTeamId};{v.BehaaldePunten}");
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            string bestandsnaam = $"F1_Jokers_Export_{DateTime.Now:yyyyMMdd_HHmm}.csv";

            return File(bytes, "text/csv", bestandsnaam);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartNieuwSeizoen()
        {
            try
            {
                _context.Voorspellingen.RemoveRange(_context.Voorspellingen);
                _context.Uitslagen.RemoveRange(_context.Uitslagen);

                _context.Kalender.RemoveRange(_context.Kalender);
                _context.WKStandCoureurs.RemoveRange(_context.WKStandCoureurs);
                _context.WKStandTeams.RemoveRange(_context.WKStandTeams);

                var gebruikers = await _context.Gebruikers.ToListAsync();
                foreach (var gebruiker in gebruikers)
                {
                    gebruiker.GebruikerPoints = 0;
                    gebruiker.Champagne = 0;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Het nieuwe seizoen is succesvol geïnitieerd! Alle data is gewist en de standen staan op 0.";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = $"Er ging iets mis bij het starten van het nieuwe seizoen: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> PuntenInstellingen()
        {
            var parameters = await _context.PuntenParameters
                .OrderBy(p => p.PuntenID)
                .ToListAsync();

            return View(parameters);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OpslaanPuntenInstellingen(List<PuntenParameter> parameters)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.PuntenParameters.UpdateRange(parameters);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "De puntentelling is succesvol bijgewerkt!";
                    return RedirectToAction("Index");
                }
                catch (System.Exception ex)
                {
                    TempData["ErrorMessage"] = $"Fout bij het opslaan van de punten: {ex.Message}";
                    return RedirectToAction("Index");
                }
            }

            TempData["ErrorMessage"] = "De ingevulde gegevens zijn ongeldig.";
            return View("PuntenInstellingen", parameters);
        }
    }
}