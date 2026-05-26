using System.ComponentModel.DataAnnotations;

namespace F1Jokers.Models
{
    public class WKStandCoureur
    {
        [Key]
        public int StartNr { get; set; }
        public int Positie { get; set; }
        public int Punten { get; set; }
        public int Overwinningen { get; set; }
    }
}