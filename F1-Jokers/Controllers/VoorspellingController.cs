using Microsoft.AspNetCore.Mvc;

namespace F1Jokers.Controllers
{
    public class VoorspellingController : Controller
    {
        public IActionResult Index()
        {
            // Later haal je hier de actuele race en coureurs uit je database
            return View();
        }
    }
}