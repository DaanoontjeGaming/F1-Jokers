using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace F1Jokers.Models
{
    [Table("Voorspellingen")]
    [PrimaryKey(nameof(GebruikerID), nameof(RaceID), nameof(TypeVoorspelling))]
    public class Voorspelling
    {
        public int GebruikerID { get; set; }

        public string RaceID { get; set; }

        public string TypeVoorspelling { get; set; }

        [Required]
        public int StartNr { get; set; }
        public int BehaaldePunten { get; set; }
    }
}