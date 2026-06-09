using System.ComponentModel.DataAnnotations;

namespace F1Jokers.Models
{
    public class Gebruiker
    {
        // --- Keys ---
        [Key]
        public int GebruikerID { get; set; }

        // --- Properties ---
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public int? GebruikerPoints { get; set; }
        public int Champagne { get; set; }
    }
}