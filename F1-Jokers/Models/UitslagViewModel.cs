using System.Collections.Generic;

namespace F1Jokers.Models
{
    public class UitslagViewModel
    {
        public string SelectedRaceId { get; set; }
        public List<Kalender> VolledigeKalender { get; set; }
        public Kalender HuidigeKalenderItem { get; set; }

        public List<CoureurResultaat> RaceTop10 { get; set; } = new List<CoureurResultaat>();
        public CoureurResultaat RacePole { get; set; }
        public CoureurResultaat SnelsteRonde { get; set; }

        public List<CoureurResultaat> SprintTop5 { get; set; } = new List<CoureurResultaat>();
        public CoureurResultaat SprintPole { get; set; }
    }

    public class CoureurResultaat
    {
        public int Positie { get; set; }
        public int Startnr { get; set; }
        public string Naam { get; set; }
        public string Team { get; set; }
    }
}