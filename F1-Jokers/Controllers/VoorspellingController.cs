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
            var model = new VoorspellingViewModel();
            model.Coureurs = _context.Coureurs.Include(c => c.Team).ToList();

            // AANGEPAST: Haal uitsluitend de normale 'Race' items op (geen Sprint, geen Seizoen)
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
                // AANGEPAST: Zorg dat de standaard ingeladen race ook altijd een 'Race' is
                model.HuidigeKalenderItem = _context.Kalender
                    .Where(k => k.Deadline > DateTime.Now && k.Racetype == "Race")
                    .OrderBy(k => k.Datum)
                    .FirstOrDefault() ?? _context.Kalender.Where(k => k.Racetype == "Race").LastOrDefault();
            }

            if (model.HuidigeKalenderItem != null)
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(userIdString, out int userId))
                {
                    string mainId = model.HuidigeKalenderItem.RaceID.Replace("S", "");
                    string sprintId = mainId + "S";
                    string seizoenId = "Seizoen" + DateTime.Now.Year.ToString();

                    // Haal alle relevante data op voor deze race en het seizoen
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
                return BadRequest(new { message = "De server kon de voorspelling niet verwerken." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int gebruikerId = int.Parse(userIdClaim.Value);

            string mainId = data.RaceId.Replace("S", "");
            string sprintId = mainId + "S";
            string seizoenId = "Seizoen" + DateTime.Now.Year.ToString();


            var oudeData = _context.Voorspellingen
                .Where(v => v.GebruikerID == gebruikerId &&
                           (v.RaceID == mainId || v.RaceID == sprintId || v.RaceID == seizoenId));

            _context.Voorspellingen.RemoveRange(oudeData);

            var nieuweLijst = new List<Voorspelling>();

            void VoegToe(string rId, string type, int? nr)
            {
                if (nr.HasValue && nr.Value > 0)
                {
                    nieuweLijst.Add(new Voorspelling
                    {
                        GebruikerID = gebruikerId,
                        RaceID = rId,
                        TypeVoorspelling = type,
                        StartNr = nr.Value
                    });
                }
            }

            // --- Race data ---
            if (!string.IsNullOrEmpty(data.RaceTop10))
            {
                var arr = data.RaceTop10.Split(',');
                for (int i = 0; i < arr.Length; i++)
                {
                    if (int.TryParse(arr[i], out int nr))
                        VoegToe(mainId, $"RacePos{i + 1}", nr);
                }
            }
            VoegToe(mainId, "RacePole", data.PolePositionStartnr);
            VoegToe(mainId, "SnelsteRonde", data.SnelsteRondeStartnr);

            // --- Sprint data ---
            if (!string.IsNullOrEmpty(data.SprintTop5))
            {
                var arr = data.SprintTop5.Split(',');
                for (int i = 0; i < arr.Length; i++)
                {
                    if (int.TryParse(arr[i], out int nr))
                        VoegToe(sprintId, $"SprintPos{i + 1}", nr);
                }
            }
            VoegToe(sprintId, "SprintPole", data.SprintPoleStartnr);

            // --- Seizoensdata (Eindstand Coureurs & Teams) ---
            if (!string.IsNullOrEmpty(data.SeizoenCoureursTop10))
            {
                var arr = data.SeizoenCoureursTop10.Split(',');
                for (int i = 0; i < arr.Length; i++)
                {
                    if (int.TryParse(arr[i], out int nr))
                        VoegToe(seizoenId, $"SeizoenCPos{i + 1}", nr);
                }
            }

            if (!string.IsNullOrEmpty(data.SeizoenTeamsTop11))
            {
                var arr = data.SeizoenTeamsTop11.Split(',');
                for (int i = 0; i < arr.Length; i++)
                {
                    if (int.TryParse(arr[i], out int nr))
                        VoegToe(seizoenId, $"SeizoenTPos{i + 1}", nr);
                }
            }

            VoegToe(seizoenId, "MeesteRaceWinst", data.MeesteRaceWinstStartnr);
            VoegToe(seizoenId, "MeesteSprintWinst", data.MeesteSprintWinstStartnr);
            VoegToe(seizoenId, "MeesteRacePoles", data.MeesteRacePolesStartnr);
            VoegToe(seizoenId, "MeesteSprintPoles", data.MeesteSprintPolesStartnr);

            _context.Voorspellingen.AddRange(nieuweLijst);
            _context.SaveChanges();

            return Ok(new { message = "Je voorspelling is succesvol bijgewerkt!" });
        }
    }
}