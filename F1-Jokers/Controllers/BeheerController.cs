using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using F1Jokers.Services;

namespace F1Jokers.Controllers
{
    // De 'uitsmijter' die controleert of de gebruiker de rol 'Beheerder' heeft
    [Authorize(Roles = "Beheerder")]
    public class BeheerController : Controller
    {
        private readonly F1ApiService _f1ApiService;
        private readonly PuntenService _puntenService;

        // Beide services worden hier succesvol binnengehaald
        public BeheerController(F1ApiService f1ApiService, PuntenService puntenService)
        {
            _f1ApiService = f1ApiService;
            _puntenService = puntenService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Beveiliging tegen CSRF-aanvallen
        public async Task<IActionResult> HaalUitslagenOp(string apiRound)
        {
            if (string.IsNullOrWhiteSpace(apiRound))
            {
                TempData["ErrorMessage"] = "Vul a.u.b. een geldig ronde-nummer in.";
                return RedirectToAction("Index");
            }

            try
            {
                // Stap 1: Haal de uitslagen op via de Jolpica API
                await _f1ApiService.HaalEnVerwerkRaceAsync(apiRound);

                // Stap 2: Bereken direct automatisch de punten voor deze race
                await _puntenService.BerekenPuntenVoorRaceAsync(apiRound);

                TempData["SuccessMessage"] = $"De uitslagen en de poule-punten voor ronde {apiRound} zijn succesvol verwerkt!";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = $"Er ging iets mis bij het verwerken: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}