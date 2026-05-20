using System.Collections.Generic;
using F1Jokers.Data; // Zorg dat dit klopt met waar jouw Kalender model staat

namespace F1Jokers.Models
{
    // 1. Het hoofd-model dat naar de pagina (View) wordt gestuurd
    public class PouleViewModel
    {
        public List<GebruikerKlassementItem> Ranglijst { get; set; } = new();
        public string AlgemenePouleNaam { get; set; } = "F1-Jokers Algemeen Klassement";

        // Voor de dropdown
        public string SelectedRaceId { get; set; }
        public List<Kalender> VolledigeKalender { get; set; } = new();
    }

    // 2. DIT IS HET ONTBREKENDE MODEL! 
    // Dit vertelt C# exact wat een 'GebruikerKlassementItem' is.
    public class GebruikerKlassementItem
    {
        public int Positie { get; set; }
        public string Username { get; set; }
        public int TotalePunten { get; set; }
        public int VerschilVorigGPWeekend { get; set; }

        public int AantalChampagneFlessen { get; set; }
    }
}