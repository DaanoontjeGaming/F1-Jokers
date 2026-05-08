using System.Collections.Generic;
using System.Globalization;

namespace F1Jokers.Models
{
    public class VoorspellingViewModel
    {
        public List<Coureur> Coureurs { get; set; }
        public List<Kalender> VolledigeKalender { get; set; }
        public Kalender HuidigeKalenderItem { get; set; }

        // NIEUW: Deze lijst bevat de reeds gemaakte voorspelling van de ingelogde gebruiker
        public List<Voorspelling> BestaandeVoorspellingen { get; set; } = new List<Voorspelling>();
    }
}