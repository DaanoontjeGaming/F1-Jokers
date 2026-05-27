using System.Collections.Generic;

namespace F1Jokers.Models
{
    public class HomeViewModel
    {
        public Kalender EerstvolgendeRace { get; set; }
        public List<CoureurStandItem> CoureursStand { get; set; } = new List<CoureurStandItem>();
        public List<TeamStandItem> TeamsStand { get; set; } = new List<TeamStandItem>();
    }

    public class CoureurStandItem
    {
        public int Positie { get; set; }
        public string Naam { get; set; }
        public int Punten { get; set; }
    }

    public class TeamStandItem
    {
        public int Positie { get; set; }
        public string Naam { get; set; }
        public int Punten { get; set; }
    }
}