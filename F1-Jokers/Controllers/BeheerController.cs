using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace F1Jokers.Controllers
{
    // De 'uitsmijter' die controleert of de gebruiker de rol 'Beheerder' heeft
    [Authorize(Roles = "Beheerder")]
    public class BeheerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}