using System.Collections.Generic;
using F1Jokers.Data; // Zorg dat dit klopt met waar jouw Kalender model staat

namespace F1Jokers.Models
{
    // 1. Het hoofd-model dat naar de pagina (View) wordt gestuurd
    public class PouleViewModel
    {
        public List<GebruikerKlassementItem> Ranglijst { get; set; } = new();
        public string AlgemenePouleNaam { get; set; } = "F1-Jokers Poule tussenstand";

        public string SelectedRaceId { get; set; }
        public List<Kalender> VolledigeKalender { get; set; } = new();
    }

    public class GebruikerKlassementItem
    {
        public int Positie { get; set; }
        public string Username { get; set; }
        public int TotalePunten { get; set; }
        public int VerschilVorigGPWeekend { get; set; }
        public int AantalChampagneFlessen { get; set; }
        public int PuntenGeselecteerdeGP { get; set; }
    }
}