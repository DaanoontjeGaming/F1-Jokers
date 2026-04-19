using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Jokers.Models
{
    [Table("Teams")]
    public class Team
    {
        [Key]
        public int TeamId { get; set; }

        public string Teamnaam { get; set; }

        public string Powerunit { get; set; }

        public int TeamPoints { get; set; }
    }
}