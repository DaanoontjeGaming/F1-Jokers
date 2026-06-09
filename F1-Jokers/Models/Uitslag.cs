using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Jokers.Models
{
    [Table("Uitslagen")]
    public class Uitslag
    {
        [Required]
        [StringLength(50)]
        public string RaceID { get; set; }

        [Required]
        [StringLength(20)]
        public string TypeResultaat { get; set; }

        public int? StartNr { get; set; }

        public int? TeamId { get; set; }
    }
}