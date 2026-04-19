using Microsoft.AspNetCore.Mvc;
using F1Jokers.Data;
using System.Linq;

namespace F1Jokers.Controllers
{
    // Deze API luistert naar /api/auth
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("inloggen")]
        public IActionResult Inloggen([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Vul a.u.b. alle velden in." });
            }

            var gebruiker = _context.Gebruikers.FirstOrDefault(g => g.Email == request.Email);

            if (gebruiker == null || gebruiker.Password != request.Password)
            {
                return Unauthorized(new { message = "E-mailadres of wachtwoord onjuist." });
            }

            return Ok(new
            {
                message = "Succesvol ingelogd!",
                username = gebruiker.Username,
                rol = gebruiker.Rol
            });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}