using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Jokers.Models
{
    [Table("Gebruikers")]
    public class Gebruiker
    {
        [Key]
        public int GebruikerID { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Email { get; set; }

        public string Rol { get; set; }

        public int? GebruikerPoints { get; set; }
    }
}