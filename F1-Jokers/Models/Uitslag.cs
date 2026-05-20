using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Jokers.Models
{
    [Table("Uitslagen")]
    public class Uitslag
    {
        [Required]
        [StringLength(11)]
        public string RaceID { get; set; }

        [Required]
        [StringLength(20)]
        public string TypeResultaat { get; set; }

        [Required]
        public int StartNr { get; set; }
    }
}