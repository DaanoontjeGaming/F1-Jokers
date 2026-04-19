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
    [Authorize]
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

            return View(model);
        }

        [HttpPost]
        public IActionResult Opslaan([FromBody] VoorspellingSubmissionDto data)
        {
            // Foutafhandeling: Controleer of de JSON succesvol is gekoppeld aan de DTO
            if (data == null || string.IsNullOrEmpty(data.RaceId))
            {
                return BadRequest(new { message = "De API kon de verzonden data niet lezen (JSON bindingsfout)." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            int gebruikerId = int.Parse(userIdClaim.Value);

            string mainId = data.RaceId.Replace("S", "");
            string sprintId = mainId + "S";

            var oudeData = _context.Voorspellingen
                .Where(v => v.GebruikerID == gebruikerId && (v.RaceID == mainId || v.RaceID == sprintId))
                .ToList();

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

            // 1. Hoofdrace opslaan
            if (!string.IsNullOrEmpty(data.RaceTop10))
            {
                var arr = data.RaceTop10.Split(',');
                for (int i = 0; i < arr.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(arr[i]) && int.TryParse(arr[i], out int nr) && nr > 0)
                        VoegToe(mainId, $"RacePos{i + 1}", nr);
                }
            }
            VoegToe(mainId, "RacePole", data.PolePositionStartnr);
            VoegToe(mainId, "SnelsteRonde", data.SnelsteRondeStartnr);

            // 2. Sprintrace opslaan
            if (!string.IsNullOrEmpty(data.SprintTop5))
            {
                var arr = data.SprintTop5.Split(',');
                for (int i = 0; i < arr.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(arr[i]) && int.TryParse(arr[i], out int nr) && nr > 0)
                        VoegToe(sprintId, $"SprintPos{i + 1}", nr);
                }
            }
            VoegToe(sprintId, "SprintPole", data.SprintPoleStartnr);

            // 3. Seizoen opslaan
            if (!string.IsNullOrEmpty(data.SeizoenCoureursTop10))
            {
                var arr = data.SeizoenCoureursTop10.Split(',');
                for (int i = 0; i < arr.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(arr[i]) && int.TryParse(arr[i], out int nr) && nr > 0)
                        VoegToe(mainId, $"SeizoenCPos{i + 1}", nr);
                }
            }

            if (!string.IsNullOrEmpty(data.SeizoenTeamsTop11))
            {
                var arr = data.SeizoenTeamsTop11.Split(',');
                for (int i = 0; i < arr.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(arr[i]) && int.TryParse(arr[i], out int nr) && nr > 0)
                        VoegToe(mainId, $"SeizoenTPos{i + 1}", nr);
                }
            }

            VoegToe(mainId, "MeesteRaceWinst", data.MeesteRaceWinstStartnr);
            VoegToe(mainId, "MeesteSprintWinst", data.MeesteSprintWinstStartnr);
            VoegToe(mainId, "MeesteRacePoles", data.MeesteRacePolesStartnr);
            VoegToe(mainId, "MeesteSprintPoles", data.MeesteSprintPolesStartnr);

            _context.Voorspellingen.AddRange(nieuweLijst);
            _context.SaveChanges();

            return Ok(new { message = "Voorspelling succesvol opgeslagen!" });
        }
    }
}