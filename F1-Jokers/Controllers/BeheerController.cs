using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using F1Jokers.Services;

namespace F1Jokers.Controllers
{
    [Authorize(Roles = "Beheerder")]
    public class BeheerController : Controller
    {
        private readonly F1ApiService _f1ApiService;

        public BeheerController(F1ApiService f1ApiService)
        {
            _f1ApiService = f1ApiService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> HaalUitslagenOp(string apiRound) // Parameter toegevoegd!
        {
            if (string.IsNullOrWhiteSpace(apiRound))
            {
                TempData["ErrorMessage"] = "Vul a.u.b. een geldig ronde-nummer in.";
                return RedirectToAction("Index");
            }

            try
            {
                // Geef apiRound door aan de service
                await _f1ApiService.HaalEnVerwerkRaceAsync(apiRound);

                TempData["SuccessMessage"] = $"De uitslagen van ronde {apiRound} zijn succesvol opgehaald via de F1 API en weggeschreven in de database!";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = $"Er ging iets mis bij het ophalen van de uitslagen: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}