using System.ComponentModel.DataAnnotations;

namespace F1Jokers.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vul een e-mailadres in.")]
        [EmailAddress(ErrorMessage = "Dit is geen geldig e-mailadres!")]
        [Display(Name = "E-mailadres")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Wachtwoord is verplicht!")]
        [DataType(DataType.Password)]
        [Display(Name = "Wachtwoord")]
        public string Password { get; set; }

        [Display(Name = "Onthoud mij")]
        public bool RememberMe { get; set; }
    }
}
