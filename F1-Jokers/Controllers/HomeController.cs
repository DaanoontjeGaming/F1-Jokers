using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using F1Jokers.Data;
using F1Jokers.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace F1Jokers.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel();

            model.EerstvolgendeRace = await _context.Kalender
                .Where(k => k.Deadline > DateTime.Now && k.Racetype == "Race")
                .OrderBy(k => k.Deadline)
                .FirstOrDefaultAsync();

            var coureursDb = await _context.Coureurs.ToListAsync();

            var wkCoureurs = await _context.WKStandCoureurs.OrderBy(w => w.Positie).Take(22).ToListAsync();

            

            var teamsDb = await _context.Teams.ToListAsync();
            var wkTeams = await _context.WKStandTeams.OrderBy(w => w.Positie).ToListAsync();

            foreach (var wk in wkTeams)
            {
                var t = teamsDb.FirstOrDefault(x => x.TeamId == wk.TeamID);
                model.TeamsStand.Add(new TeamStandItem
                {
                    Positie = wk.Positie,
                    Naam = t != null ? t.Teamnaam : "Onbekend",
                    Punten = wk.Punten
                });
            }

            foreach (var wk in wkCoureurs)
            {
                var c = coureursDb.FirstOrDefault(x => x.Startnr == wk.StartNr);
                model.CoureursStand.Add(new CoureurStandItem
                {
                    Positie = wk.Positie,
                    Naam = c != null ? $"{c.Voornaam} {c.Achternaam}" : "Onbekend",
                    Punten = wk.Punten
                });
            }

            return View(model);
        }
    }
}