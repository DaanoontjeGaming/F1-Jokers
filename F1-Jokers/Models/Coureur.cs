using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Jokers.Models
{
    [Table("Coureurs")]
    public class Coureur
    {
        [Key]
        public int Startnr { get; set; }
        public string Voornaam { get; set; }
        public string Achternaam{ get; set; }
        public int TeamId { get; set; }
        public int? DriverPoints { get; set; }
        public Team Team { get; set; }
    }
}