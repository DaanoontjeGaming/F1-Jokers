using F1Jokers.Data;
using F1Jokers.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace F1Jokers.Controllers
{
    public class VoorspellingController : Controller
    {
        private readonly AppDbContext _context;

        public VoorspellingController(AppDbContext context)
        {
            _context = context;
        }

        // raceId is nu een string geworden, omdat je ID's als "11S" gebruikt
        public IActionResult Index(string? raceId)
        {
            var coureurs = _context.Coureurs.Include(c => c.Team).ToList();
            var kalenderItems = _context.Kalender.OrderBy(k => k.Deadline).ToList();

            Kalender huidigItem = null;
            if (kalenderItems.Any())
            {
                if (!string.IsNullOrEmpty(raceId))
                {
                    huidigItem = kalenderItems.FirstOrDefault(k => k.RaceID == raceId);
                }
                else
                {
                    huidigItem = kalenderItems.FirstOrDefault(k => k.Deadline > System.DateTime.Now) ?? kalenderItems.Last();
                }
            }

            var viewModel = new VoorspellingViewModel
            {
                Coureurs = coureurs,
                VolledigeKalender = kalenderItems,
                HuidigeKalenderItem = huidigItem
            };

            return View(viewModel);
        }
    }
}