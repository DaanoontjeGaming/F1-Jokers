using System.Collections.Generic;

namespace F1Jokers.Models
{
    // --- ViewModels ---
    public class PouleViewModel
    {
        public string AlgemenePouleNaam { get; set; }
        public string SelectedRaceId { get; set; }
        public List<Kalender> VolledigeKalender { get; set; }
        public List<GebruikerKlassementItem> Ranglijst { get; set; }
    }

    // --- Items ---
    public class GebruikerKlassementItem
    {
        public int Positie { get; set; }
        public string Username { get; set; }
        public int TotalePunten { get; set; }
        public int VerschilVorigGPWeekend { get; set; }
        public int PuntenGeselecteerdeGP { get; set; }
        public int AantalChampagneFlessen { get; set; }
    }
}