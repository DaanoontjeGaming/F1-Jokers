using Microsoft.AspNetCore.Mvc;
using F1Jokers.Models;
using F1Jokers.Data;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System;

namespace F1Jokers.Controllers
{
    public class UitslagenController : Controller
    {
        private readonly AppDbContext _context;

        public UitslagenController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string raceId)
        {
            var model = new UitslagViewModel();

            model.VolledigeKalender = _context.Kalender
                .Where(k => !k.RaceID.StartsWith("Seizoen") && !k.RaceID.EndsWith("S"))
                .OrderBy(k => k.Datum)
                .ToList();

            if (!model.VolledigeKalender.Any()) return View(model);

            if (string.IsNullOrEmpty(raceId))
            {
                var laatstVerwerkt = model.VolledigeKalender
                    .Where(k => k.IsVerwerkt)
                    .OrderByDescending(k => k.Datum)
                    .FirstOrDefault();

                raceId = laatstVerwerkt?.RaceID ?? model.VolledigeKalender.First().RaceID;
            }

            model.SelectedRaceId = raceId;
            model.HuidigeKalenderItem = model.VolledigeKalender.FirstOrDefault(k => k.RaceID == raceId);

            var dbUitslagen = _context.Uitslagen
                .Where(u => u.RaceID == raceId || u.RaceID == raceId + "S")
                .ToList();

            var coureurs = _context.Coureurs.Include(c => c.Team).ToList();

            CoureurResultaat MaakResultaat(Uitslag u, int pos = 0)
            {
                var c = coureurs.FirstOrDefault(x => x.Startnr == u.StartNr);
                return new CoureurResultaat
                {
                    Positie = pos,
                    Startnr = u.StartNr,
                    Naam = c != null ? $"{c.Voornaam} {c.Achternaam}" : "Onbekend",
                    Team = c?.Team?.Teamnaam ?? "Onbekend"
                };
            }

            foreach (var u in dbUitslagen)
            {
                if (u.TypeResultaat.StartsWith("RacePos"))
                {
                    if (int.TryParse(u.TypeResultaat.Replace("RacePos", ""), out int pos))
                        model.RaceTop10.Add(MaakResultaat(u, pos));
                }
                else if (u.TypeResultaat.StartsWith("SprintPos"))
                {
                    if (int.TryParse(u.TypeResultaat.Replace("SprintPos", ""), out int pos))
                        model.SprintTop5.Add(MaakResultaat(u, pos));
                }
                else if (u.TypeResultaat == "RacePole") model.RacePole = MaakResultaat(u);
                else if (u.TypeResultaat == "SnelsteRonde") model.SnelsteRonde = MaakResultaat(u);
                else if (u.TypeResultaat == "SprintPole") model.SprintPole = MaakResultaat(u);
            }

            model.RaceTop10 = model.RaceTop10.OrderBy(r => r.Positie).ToList();
            model.SprintTop5 = model.SprintTop5.OrderBy(r => r.Positie).ToList();

            return View(model);
        }
    }
}