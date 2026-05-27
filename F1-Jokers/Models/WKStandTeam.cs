using System.ComponentModel.DataAnnotations;

namespace F1Jokers.Models
{
    public class WKStandTeam
    {
        [Key]
        public int TeamID { get; set; }
        public int Positie { get; set; }
        public int Punten { get; set; }
        public int Overwinningen { get; set; }
    }
}