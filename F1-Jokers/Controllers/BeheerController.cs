using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using F1Jokers.Services;

namespace F1Jokers.Controllers
{
    [Authorize(Roles = "Beheerder")]
    public class BeheerController : Controller
    {
        // --- Fields ---
        private readonly F1ApiService _f1ApiService;
        private readonly PuntenService _puntenService;

        // --- Constructor ---
        public BeheerController(F1ApiService f1ApiService, PuntenService puntenService)
        {
            _f1ApiService = f1ApiService;
            _puntenService = puntenService;
        }

        // --- Actions ---
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HaalUitslagenOp(string apiRound)
        {
            if (string.IsNullOrWhiteSpace(apiRound))
            {
                TempData["ErrorMessage"] = "Vul a.u.b. een geldig RaceID in.";
                return RedirectToAction("Index");
            }

            try
            {
                string lokaalRaceId = await _f1ApiService.HaalEnVerwerkRaceAsync(apiRound);
                await _puntenService.BerekenPuntenVoorRaceAsync(lokaalRaceId);
                await _puntenService.BerekenPuntenVoorRaceAsync(lokaalRaceId + "S");

                TempData["SuccessMessage"] = $"De uitslagen, punten én flessen zijn succesvol verwerkt voor Grand Prix {apiRound} (Lokaal ID: {lokaalRaceId})!";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = $"Er ging iets mis bij het verwerken: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}