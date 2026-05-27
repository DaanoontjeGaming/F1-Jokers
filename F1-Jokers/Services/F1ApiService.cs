using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using F1Jokers.Data;
using F1Jokers.Models;

namespace F1Jokers.Services
{
    public class F1ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;

        public F1ApiService(HttpClient httpClient, AppDbContext context)
        {
            _httpClient = httpClient;
            _context = context;
        }

        public async Task<string> HaalEnVerwerkRaceAsync(string raceId)
        {
            string apiRound = raceId;

            if (DateTime.Now.Year == 2026 && int.TryParse(raceId, out int lokaalId) && lokaalId >= 6)
            {
                apiRound = (lokaalId - 2).ToString();
            }

            var resultsResponse = await _httpClient.GetStringAsync($"https://api.jolpi.ca/ergast/f1/current/{apiRound}/results.json");
            using var resultsDoc = JsonDocument.Parse(resultsResponse);

            var racesElement = resultsDoc.RootElement.GetProperty("MRData").GetProperty("RaceTable").GetProperty("Races");

            if (racesElement.GetArrayLength() == 0)
            {
                throw new Exception($"Geen uitslagen gevonden voor API ronde {apiRound}.");
            }

            var raceNode = racesElement[0];
            string apiDateString = raceNode.GetProperty("date").GetString();
            DateTime apiDate = DateTime.Parse(apiDateString);

            var kalenderRace = _context.Kalender.FirstOrDefault(k => k.Datum.Date == apiDate.Date);
            if (kalenderRace == null)
            {
                throw new Exception($"Kan race met datum {apiDateString} niet vinden in de lokale database.");
            }

            string correcteRaceId = kalenderRace.RaceID;

            var bestaandeUitslagen = _context.Uitslagen.Where(u => u.RaceID == correcteRaceId).ToList();
            if (bestaandeUitslagen.Any())
            {
                _context.Uitslagen.RemoveRange(bestaandeUitslagen);
                await _context.SaveChangesAsync();
            }

            var resultsArray = raceNode.GetProperty("Results");
            foreach (var result in resultsArray.EnumerateArray())
            {
                int positie = int.Parse(result.GetProperty("position").GetString());
                int startnr = int.Parse(result.GetProperty("number").GetString());

                if (positie <= 22)
                {
                    _context.Uitslagen.Add(new Uitslag { RaceID = correcteRaceId, TypeResultaat = $"RacePos{positie}", StartNr = startnr });
                }

                if (result.TryGetProperty("FastestLap", out var fastestLap) && fastestLap.TryGetProperty("rank", out var rank) && rank.GetString() == "1")
                {
                    _context.Uitslagen.Add(new Uitslag { RaceID = correcteRaceId, TypeResultaat = "SnelsteRonde", StartNr = startnr });
                }
            }

            try
            {
                var qualiResponse = await _httpClient.GetStringAsync($"https://api.jolpi.ca/ergast/f1/current/{apiRound}/qualifying.json");
                using var qualiDoc = JsonDocument.Parse(qualiResponse);
                var qualiRaces = qualiDoc.RootElement.GetProperty("MRData").GetProperty("RaceTable").GetProperty("Races");
                if (qualiRaces.GetArrayLength() > 0)
                {
                    var qualiArray = qualiRaces[0].GetProperty("QualifyingResults");
                    if (qualiArray.GetArrayLength() > 0)
                    {
                        int poleStartnr = int.Parse(qualiArray[0].GetProperty("number").GetString());
                        _context.Uitslagen.Add(new Uitslag { RaceID = correcteRaceId, TypeResultaat = "RacePole", StartNr = poleStartnr });
                    }
                }
            }
            catch { }

            if (kalenderRace.HasSprintRace)
            {
                string sprintRaceId = correcteRaceId + "S";

                var bestaandeSprintUitslagen = _context.Uitslagen.Where(u => u.RaceID == sprintRaceId).ToList();
                if (bestaandeSprintUitslagen.Any())
                {
                    _context.Uitslagen.RemoveRange(bestaandeSprintUitslagen);
                    await _context.SaveChangesAsync();
                }

                try
                {
                    var sprintResponse = await _httpClient.GetStringAsync($"https://api.jolpi.ca/ergast/f1/current/{apiRound}/sprint.json");
                    using var sprintDoc = JsonDocument.Parse(sprintResponse);
                    var sprintRacesNode = sprintDoc.RootElement.GetProperty("MRData").GetProperty("RaceTable").GetProperty("Races");

                    if (sprintRacesNode.GetArrayLength() > 0)
                    {
                        var sprintArray = sprintRacesNode[0].GetProperty("SprintResults");

                        foreach (var sprintRes in sprintArray.EnumerateArray())
                        {
                            int positie = int.Parse(sprintRes.GetProperty("position").GetString());
                            int startnr = int.Parse(sprintRes.GetProperty("number").GetString());

                            if (positie <= 22)
                            {
                                _context.Uitslagen.Add(new Uitslag { RaceID = sprintRaceId, TypeResultaat = $"SprintPos{positie}", StartNr = startnr });
                            }

                            if (sprintRes.TryGetProperty("grid", out var grid) && grid.GetString() == "1")
                            {
                                _context.Uitslagen.Add(new Uitslag { RaceID = sprintRaceId, TypeResultaat = "SprintPole", StartNr = startnr });
                            }
                        }
                    }
                }
                catch { }
            }

            await _context.SaveChangesAsync();

            // ================= WK STAND COUREURS =================
            try
            {
                var driverStandingsResp = await _httpClient.GetStringAsync($"https://api.jolpi.ca/ergast/f1/current/{apiRound}/driverStandings.json");
                using var driverStandingsDoc = JsonDocument.Parse(driverStandingsResp);
                var driverLists = driverStandingsDoc.RootElement.GetProperty("MRData").GetProperty("StandingsTable").GetProperty("StandingsLists");

                if (driverLists.GetArrayLength() > 0)
                {
                    var oudeCoureurStanden = _context.WKStandCoureurs.ToList();
                    if (oudeCoureurStanden.Any())
                    {
                        _context.WKStandCoureurs.RemoveRange(oudeCoureurStanden);
                        await _context.SaveChangesAsync();
                    }

                    var driverStandings = driverLists[0].GetProperty("DriverStandings");
                    foreach (var ds in driverStandings.EnumerateArray())
                    {
                        int wkPositie = int.Parse(ds.GetProperty("position").GetString());
                        int wkWins = int.Parse(ds.GetProperty("wins").GetString());
                        int wkPunten = (int)Math.Round(double.Parse(ds.GetProperty("points").GetString(), System.Globalization.CultureInfo.InvariantCulture));

                        if (ds.GetProperty("Driver").TryGetProperty("permanentNumber", out var permNrStr) && int.TryParse(permNrStr.GetString(), out int startnr))
                        {
                            _context.WKStandCoureurs.Add(new WKStandCoureur { StartNr = startnr, Positie = wkPositie, Punten = wkPunten, Overwinningen = wkWins });
                        }
                    }
                }
            }
            catch { }

            // ================= WK STAND TEAMS =================
            try
            {
                var teamStandingsResp = await _httpClient.GetStringAsync($"https://api.jolpi.ca/ergast/f1/current/{apiRound}/constructorStandings.json");
                using var teamStandingsDoc = JsonDocument.Parse(teamStandingsResp);
                var teamLists = teamStandingsDoc.RootElement.GetProperty("MRData").GetProperty("StandingsTable").GetProperty("StandingsLists");

                if (teamLists.GetArrayLength() > 0)
                {
                    var oudeTeamStanden = _context.WKStandTeams.ToList();
                    if (oudeTeamStanden.Any())
                    {
                        _context.WKStandTeams.RemoveRange(oudeTeamStanden);
                        await _context.SaveChangesAsync();
                    }

                    var teamsInDb = _context.Teams.ToList();
                    var constructorStandings = teamLists[0].GetProperty("ConstructorStandings");

                    foreach (var cs in constructorStandings.EnumerateArray())
                    {
                        int wkPositie = int.Parse(cs.GetProperty("position").GetString());
                        int wkWins = int.Parse(cs.GetProperty("wins").GetString());
                        int wkPunten = (int)Math.Round(double.Parse(cs.GetProperty("points").GetString(), System.Globalization.CultureInfo.InvariantCulture));

                        var constructorNode = cs.GetProperty("Constructor");
                        string apiTeamName = constructorNode.GetProperty("name").GetString().ToLower();
                        string constructorId = constructorNode.TryGetProperty("constructorId", out var cId) ? cId.GetString().ToLower() : "";

                        // --- KOGELVRIJE FIX VOOR RACING BULLS ---
                        if (constructorId == "rb" || apiTeamName.Contains("rb f1") || apiTeamName.Contains("visa"))
                        {
                            var rbMatch = teamsInDb.FirstOrDefault(t => t.Teamnaam.ToLower().Contains("racing bulls") || t.Teamnaam.ToLower() == "rb");
                            if (rbMatch != null)
                            {
                                _context.WKStandTeams.Add(new WKStandTeam { TeamID = rbMatch.TeamId, Positie = wkPositie, Punten = wkPunten, Overwinningen = wkWins });
                                continue;
                            }
                        }

                        var match = teamsInDb.FirstOrDefault(t => t.Teamnaam.ToLower().Contains(apiTeamName) || apiTeamName.Contains(t.Teamnaam.ToLower()));

                        if (match != null)
                        {
                            _context.WKStandTeams.Add(new WKStandTeam { TeamID = match.TeamId, Positie = wkPositie, Punten = wkPunten, Overwinningen = wkWins });
                        }
                    }
                }
            }
            catch { }

            await _context.SaveChangesAsync();

            return correcteRaceId;
        }
    }
}