using Microsoft.AspNetCore.Mvc;
using F1Jokers.Data; // Pas dit aan naar jouw eigen namespace als deze anders is
using F1Jokers.Models; // Nodig voor het Kalender model
using System;
using System.Linq;

namespace F1Jokers.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var volgendeRace = _context.Kalender
                .Where(k => k.Datum >= DateTime.Today && k.Racetype == "Race")
                .OrderBy(k => k.Datum)
                .FirstOrDefault();

            ViewBag.VolgendeRace = volgendeRace;

            return View();
        }
    }
}