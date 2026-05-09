using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Jokers.Models
{
    [Table("Kalender")]
    public class Kalender
    {
        [Key]
        public string RaceID { get; set; }

        [Required]
        public string Racenaam { get; set; }

        [Required]
        public DateTime Datum { get; set; }

        [Required]
        public DateTime Deadline { get; set; }

        [Required]
        public string Racetype { get; set; }
        [Required]
        public bool HasSprintRace { get; set; }
        public bool IsVerwerkt { get; set; } = false;
    }
}