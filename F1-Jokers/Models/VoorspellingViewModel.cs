using System.Collections.Generic;
using System.Globalization;

namespace F1Jokers.Models
{
    public class VoorspellingViewModel
    {
        // De lijst met coureurs voor alle drag-and-drop en dropdowns
        public List<Coureur> Coureurs { get; set; }

        // De volledige kalender voor de Grand Prix switcher
        public List<Kalender> VolledigeKalender { get; set; }

        // De specifieke Grand Prix die nu bekeken wordt
        public Kalender HuidigeKalenderItem { get; set; }
    }
}