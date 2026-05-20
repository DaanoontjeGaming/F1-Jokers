using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
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

        public async Task HaalEnVerwerkLaatsteRaceAsync()
        {
            // 1. Haal de Race Resultaten op
            var resultsResponse = await _httpClient.GetStringAsync("https://api.jolpi.ca/ergast/f1/current/last/results.json");
            using var resultsDoc = JsonDocument.Parse(resultsResponse);

            var raceNode = resultsDoc.RootElement
                .GetProperty("MRData")
                .GetProperty("RaceTable")
                .GetProperty("Races")[0];

            // Om de juiste endpoints voor Quali en Sprint aan te roepen, gebruiken we de API-ronde (bijv. "4")
            string apiRound = raceNode.GetProperty("round").GetString();

            // We lezen de datum van de race uit de API en koppelen die aan jouw database
            string apiDateString = raceNode.GetProperty("date").GetString();
            DateTime apiDate = DateTime.Parse(apiDateString);

            // Zoek de race in jouw kalender met dezelfde datum
            var kalenderRace = _context.Kalender.FirstOrDefault(k => k.Datum.Date == apiDate.Date);
            if (kalenderRace == null)
            {
                throw new Exception($"Kan race met datum {apiDateString} niet vinden in de lokale database.");
            }

            // Vanaf hier gebruiken we uitsluitend JOUW unieke RaceID (bijv. "6" voor Miami)
            string correcteRaceId = kalenderRace.RaceID;

            // Gooi eventueel al bestaande uitslagen van deze HOOFD-race weg om dubbelingen te voorkomen
            var bestaandeUitslagen = _context.Uitslagen.Where(u => u.RaceID == correcteRaceId).ToList();
            if (bestaandeUitslagen.Any())
            {
                _context.Uitslagen.RemoveRange(bestaandeUitslagen);
                await _context.SaveChangesAsync();
            }

            // 2. Verwerk Race Posities (1 t/m 10) en Snelste Ronde
            var resultsArray = raceNode.GetProperty("Results");
            foreach (var result in resultsArray.EnumerateArray())
            {
                int positie = int.Parse(result.GetProperty("position").GetString());
                int startNr = int.Parse(result.GetProperty("number").GetString());

                if (positie <= 10)
                {
                    _context.Uitslagen.Add(new Uitslag
                    {
                        RaceID = correcteRaceId,
                        TypeResultaat = $"RacePos{positie}",
                        StartNr = startNr
                    });
                }

                // Controleer op snelste ronde in de race 
                if (result.TryGetProperty("FastestLap", out var fastestLap) &&
                    fastestLap.TryGetProperty("rank", out var rank) &&
                    rank.GetString() == "1")
                {
                    _context.Uitslagen.Add(new Uitslag
                    {
                        RaceID = correcteRaceId,
                        TypeResultaat = "SnelsteRonde",
                        StartNr = startNr
                    });
                }
            }

            // 3. Haal de Kwalificatie Resultaten op (voor Race Pole)
            var qualiResponse = await _httpClient.GetStringAsync($"https://api.jolpi.ca/ergast/f1/current/{apiRound}/qualifying.json");
            using var qualiDoc = JsonDocument.Parse(qualiResponse);

            var qualiArray = qualiDoc.RootElement.GetProperty("MRData").GetProperty("RaceTable").GetProperty("Races")[0].GetProperty("QualifyingResults");

            // De winnaar van de kwali staat altijd op index 0
            int poleStartNr = int.Parse(qualiArray[0].GetProperty("number").GetString());

            _context.Uitslagen.Add(new Uitslag
            {
                RaceID = correcteRaceId,
                TypeResultaat = "RacePole",
                StartNr = poleStartNr
            });

            // 4. Controleer in jouw Kalender of dit een sprint-weekend was
            if (kalenderRace.HasSprintRace)
            {
                // Maak het speciale Sprint-ID aan (bijv. "6S")
                string sprintRaceId = correcteRaceId + "S";

                // Gooi eventueel al bestaande sprint-uitslagen weg om dubbelingen te voorkomen
                var bestaandeSprintUitslagen = _context.Uitslagen.Where(u => u.RaceID == sprintRaceId).ToList();
                if (bestaandeSprintUitslagen.Any())
                {
                    _context.Uitslagen.RemoveRange(bestaandeSprintUitslagen);
                    await _context.SaveChangesAsync();
                }

                // Haal specifiek de sprint van deze API-ronde op
                var sprintResponse = await _httpClient.GetStringAsync($"https://api.jolpi.ca/ergast/f1/current/{apiRound}/sprint.json");
                using var sprintDoc = JsonDocument.Parse(sprintResponse);

                var sprintRacesNode = sprintDoc.RootElement.GetProperty("MRData").GetProperty("RaceTable").GetProperty("Races");

                if (sprintRacesNode.GetArrayLength() > 0)
                {
                    var sprintArray = sprintRacesNode[0].GetProperty("SprintResults");

                    foreach (var sprintRes in sprintArray.EnumerateArray())
                    {
                        int positie = int.Parse(sprintRes.GetProperty("position").GetString());
                        int startNr = int.Parse(sprintRes.GetProperty("number").GetString());

                        // Sla nu alleen de top 5 op
                        if (positie <= 5)
                        {
                            _context.Uitslagen.Add(new Uitslag
                            {
                                RaceID = sprintRaceId, // Gebruik hier het 6S ID
                                TypeResultaat = $"SprintPos{positie}",
                                StartNr = startNr
                            });
                        }

                        // Sprint Pole (degene die als 1e mocht starten in de sprintrace)
                        if (sprintRes.TryGetProperty("grid", out var grid) && grid.GetString() == "1")
                        {
                            _context.Uitslagen.Add(new Uitslag
                            {
                                RaceID = sprintRaceId, // Gebruik hier het 6S ID
                                TypeResultaat = "SprintPole",
                                StartNr = startNr
                            });
                        }
                    }
                }
            }

            // 5. Sla alle nieuwe uitslagen op in jouw database!
            await _context.SaveChangesAsync();
        }
    }
}