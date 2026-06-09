using Microsoft.AspNetCore.Mvc;
using F1Jokers.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using F1Jokers.Data;
using System;

namespace F1Jokers.Controllers
{
    public class VoorspellingController : Controller
    {
        private readonly AppDbContext _context;

        public VoorspellingController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index(string raceId)
        {
            VoorspellingViewModel model = new VoorspellingViewModel();
            model.Coureurs = _context.Coureurs.Include(c => c.Team).ToList();

            model.VolledigeKalender = _context.Kalender
                .Where(k => k.Racetype == "Race")
                .OrderBy(k => k.Datum)
                .ToList();

            if (!string.IsNullOrEmpty(raceId))
            {
                model.HuidigeKalenderItem = _context.Kalender.FirstOrDefault(k => k.RaceID == raceId);
            }
            else
            {
                model.HuidigeKalenderItem = _context.Kalender
                    .Where(k => k.Deadline > DateTime.Now && k.Racetype == "Race")
                    .OrderBy(k => k.Datum)
                    .FirstOrDefault() ?? _context.Kalender.Where(k => k.Racetype == "Race").LastOrDefault();
            }

            if (model.HuidigeKalenderItem != null)
            {
                Claim? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    string mainId = model.HuidigeKalenderItem.RaceID.Replace("S", "");
                    string sprintId = mainId + "S";
                    string seizoenId = "Seizoen" + DateTime.Now.Year.ToString();

                    model.BestaandeVoorspellingen = _context.Voorspellingen
                        .Where(v => v.GebruikerID == userId &&
                                   (v.RaceID == mainId || v.RaceID == sprintId || v.RaceID == seizoenId))
                        .ToList();
                }
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Opslaan([FromBody] VoorspellingSubmissionDto data)
        {
            if (data == null || string.IsNullOrEmpty(data.RaceId))
            {
                return BadRequest(new { message = "Ongeldige gegevens ontvangen." });
            }

            Claim? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int gebruikerId = int.Parse(userIdClaim.Value);

            string mainId = data.RaceId.Replace("S", "");
            string sprintId = mainId + "S";
            string seizoenId = "Seizoen" + DateTime.Now.Year.ToString();

            Kalender? huidigeRace = _context.Kalender.FirstOrDefault(k => k.RaceID == mainId);
            Kalender? huidigeSeizoen = _context.Kalender.FirstOrDefault(k => k.RaceID == seizoenId);
            bool isAdmin = User.IsInRole("Beheerder");

            if (!isAdmin && huidigeRace != null && DateTime.Now > huidigeRace.Deadline)
            {
                return BadRequest(new { message = "De deadline voor deze race is verstreken. Je kunt niets meer opslaan." });
            }

            IQueryable<Voorspelling> oudeDataQuery = _context.Voorspellingen.Where(v => v.GebruikerID == gebruikerId &&
                (v.RaceID == mainId || v.RaceID == sprintId || v.RaceID == seizoenId));

            List<Voorspelling> oudeDataLijst = oudeDataQuery.ToList();

            if (!isAdmin && huidigeSeizoen != null && DateTime.Now > huidigeSeizoen.Deadline)
            {
                oudeDataLijst = oudeDataLijst.Where(v => !v.RaceID.StartsWith("Seizoen")).ToList();
            }

            _context.Voorspellingen.RemoveRange(oudeDataLijst);

            List<Voorspelling> nieuweLijst = new List<Voorspelling>();

            // --- VERNIEUWDE VOEGTOE METHODE ---
            void VoegToe(string rId, string type, int? nr)
            {
                if (nr.HasValue && nr.Value > 0)
                {
                    bool isTeamVoorspelling = type.Contains("TPos");

                    nieuweLijst.Add(new Voorspelling
                    {
                        GebruikerID = gebruikerId,
                        RaceID = rId,
                        TypeVoorspelling = type,
                        StartNr = isTeamVoorspelling ? null : nr.Value,
                        TeamId = isTeamVoorspelling ? nr.Value : null
                    });
                }
            }

            // 1. Race Voorspellingen
            if (!string.IsNullOrEmpty(data.RaceTop10))
            {
                string[] arr = data.RaceTop10.Split(',');
                for (int i = 0; i < arr.Length; i++)
                {
                    if (int.TryParse(arr[i], out int nr)) VoegToe(mainId, $"RacePos{i + 1}", nr);
                }
            }
            VoegToe(mainId, "RacePole", data.PolePositionStartnr);
            VoegToe(mainId, "SnelsteRonde", data.SnelsteRondeStartnr);

            // 2. Sprint Voorspellingen
            if (!string.IsNullOrEmpty(data.SprintTop5))
            {
                string[] arr = data.SprintTop5.Split(',');
                for (int i = 0; i < arr.Length; i++)
                {
                    if (int.TryParse(arr[i], out int nr)) VoegToe(sprintId, $"SprintPos{i + 1}", nr);
                }
            }
            VoegToe(sprintId, "SprintPole", data.SprintPoleStartnr);

            // 3. Seizoensvoorspellingen
            if (isAdmin || (huidigeSeizoen != null && DateTime.Now <= huidigeSeizoen.Deadline))
            {
                if (!string.IsNullOrEmpty(data.SeizoenCoureursTop10))
                {
                    string[] arr = data.SeizoenCoureursTop10.Split(',');
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (int.TryParse(arr[i], out int nr)) VoegToe(seizoenId, $"SeizoenCPos{i + 1}", nr);
                    }
                }

                if (!string.IsNullOrEmpty(data.SeizoenTeamsTop11))
                {
                    string[] arr = data.SeizoenTeamsTop11.Split(',');
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (int.TryParse(arr[i], out int nr)) VoegToe(seizoenId, $"SeizoenTPos{i + 1}", nr);
                    }
                }

                VoegToe(seizoenId, "MeesteRaceWinst", data.MeesteRaceWinstStartnr);
                VoegToe(seizoenId, "MeesteSprintWinst", data.MeesteSprintWinstStartnr);
                VoegToe(seizoenId, "MeesteRacePoles", data.MeesteRacePolesStartnr);
                VoegToe(seizoenId, "MeesteSprintPoles", data.MeesteSprintPolesStartnr);
            }

            _context.Voorspellingen.AddRange(nieuweLijst);
            _context.SaveChanges();

            return Ok(new { message = "Je voorspelling is succesvol bijgewerkt!" });
        }
    }
}