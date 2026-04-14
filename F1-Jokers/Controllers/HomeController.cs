using F1Jokers.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace F1Jokers.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var model = new VoorspellingViewModel();
            model.RaceNaam = "Zandvoort 2026";

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
