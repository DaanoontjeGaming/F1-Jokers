using System.ComponentModel.DataAnnotations;

namespace F1Jokers.Models
{
    public class ProfielViewModel
    {
        [Required(ErrorMessage = "Gebruikersnaam is verplicht.")]
        [MinLength(3, ErrorMessage = "Gebruikersnaam moet minimaal 3 tekens lang zijn.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "E-mailadres is verplicht.")]
        [EmailAddress(ErrorMessage = "Vul een geldig e-mailadres in.")]
        public string Email { get; set; }

        public string HuidigWachtwoord { get; set; }

        [MinLength(6, ErrorMessage = "Het nieuwe wachtwoord moet minimaal 6 tekens lang zijn.")]
        public string NieuwWachtwoord { get; set; }

        [Compare("NieuwWachtwoord", ErrorMessage = "De nieuwe wachtwoorden komen niet overeen.")]
        public string BevestigNieuwWachtwoord { get; set; }
    }
}