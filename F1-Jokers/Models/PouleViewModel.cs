using System.Collections.Generic;

namespace F1Jokers.Models
{
    public class PouleViewModel
    {
        public List<GebruikerKlassementItem> Ranglijst { get; set; } = new();
        public string AlgemenePouleNaam { get; set; } = "F1-Jokers Algemeen Klassement";
    }

    public class GebruikerKlassementItem
    {
        public int Positie { get; set; }
        public string Username { get; set; }
        public int TotalePunten { get; set; }
        public int VerschilVorigGPWeekend { get; set; }
    }
}