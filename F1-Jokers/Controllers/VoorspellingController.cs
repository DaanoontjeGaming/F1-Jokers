using Microsoft.AspNetCore.Mvc;
using F1Jokers.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using F1Jokers.Data;

namespace F1Jokers.Controllers
{
    
    public class VoorspellingController : Controller
    {
        // =========================================================================
        // PROPERTIES & CONSTRUCTOR
        // =========================================================================
        private readonly AppDbContext _context;

        public VoorspellingController(AppDbContext context)
        {
            _context = context;
        }


        // =========================================================================
        // HTTP GET - PAGE RENDERING
        // =========================================================================
        [HttpGet]
        public IActionResult Index(string raceId)
        {
            var model = new VoorspellingViewModel();
            model.Coureurs = _context.Coureurs.Include(c => c.Team).ToList();
            model.VolledigeKalender = _context.Kalender.OrderBy(k => k.Datum).ToList();

            if (!string.IsNullOrEmpty(raceId))
            {
                model.HuidigeKalenderItem = _context.Kalender.FirstOrDefault(k => k.RaceID == raceId);
            }
            else
            {
                model.HuidigeKalenderItem = _context.Kalender
                    .Where(k => k.Deadline > System.DateTime.Now)
                    .OrderBy(k => k.Datum)
                    .FirstOrDefault() ?? _context.Kalender.LastOrDefault();
            }

            if (model.HuidigeKalenderItem != null)
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(userIdString, out int userId))
                {
                    string mainId = model.HuidigeKalenderItem.RaceID.Replace("S", "");
                    string sprintId = mainId + "S";

                    model.BestaandeVoorspellingen = _context.Voorspellingen
                        .Where(v => v.GebruikerID == userId && (v.RaceID == mainId || v.RaceID == sprintId))
                        .ToList();
                }
            }

            return View(model);
        }


        // =========================================================================
        // HTTP POST - API ENDPOINT (SAVE/UPDATE)
        // =========================================================================
        [HttpPost]
        [Authorize]
        public IActionResult Opslaan([FromBody] VoorspellingSubmissionDto data)
        {
            // -----------------------------------------------------------
            // 1. VALIDATION & SETUP
            // -----------------------------------------------------------
            if (data == null || string.IsNullOrEmpty(data.RaceId))
            {
                return BadRequest(new { message = "De server kon de voorspelling niet verwerken." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int gebruikerId = int.Parse(userIdClaim.Value);

            string mainId = data.RaceId.Replace("S", "");
            string sprintId = mainId + "S";

            // -----------------------------------------------------------
            // 2. DATABASE PREPARATION (WIPE)
            // -----------------------------------------------------------
            var oudeData = _context.Voorspellingen
                .Where(v => v.GebruikerID == gebruikerId && (v.RaceID == mainId || v.RaceID == sprintId));

            _context.Voorspellingen.RemoveRange(oudeData);

            // -----------------------------------------------------------
            // 3. DATA MAPPING (LOCAL HELPER & ASSIGNMENT)
            // -----------------------------------------------------------
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

            if (!string.IsNullOrEmpty(data.SeizoenCoureursTop10))
            {
                var arr = data.SeizoenCoureursTop10.Split(',');
                for (int i = 0; i < arr.Length; i++)
                {
                    if (int.TryParse(arr[i], out int nr))
                        VoegToe(mainId, $"SeizoenCPos{i + 1}", nr);
                }
            }

            if (!string.IsNullOrEmpty(data.SeizoenTeamsTop11))
            {
                var arr = data.SeizoenTeamsTop11.Split(',');
                for (int i = 0; i < arr.Length; i++)
                {
                    if (int.TryParse(arr[i], out int nr))
                        VoegToe(mainId, $"SeizoenTPos{i + 1}", nr);
                }
            }

            VoegToe(mainId, "MeesteRaceWinst", data.MeesteRaceWinstStartnr);
            VoegToe(mainId, "MeesteSprintWinst", data.MeesteSprintWinstStartnr);
            VoegToe(mainId, "MeesteRacePoles", data.MeesteRacePolesStartnr);
            VoegToe(mainId, "MeesteSprintPoles", data.MeesteSprintPolesStartnr);

            // -----------------------------------------------------------
            // 4. DATABASE COMMIT
            // -----------------------------------------------------------
            _context.Voorspellingen.AddRange(nieuweLijst);
            _context.SaveChanges();

            return Ok(new { message = "Je voorspelling is succesvol bijgewerkt!" });
        }
    }
}