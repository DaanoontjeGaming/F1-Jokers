using F1Jokers.Data;
using F1Jokers.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace F1Jokers.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Inloggen()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inloggen(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _context.Gebruikers.FirstOrDefault(u => u.Username == model.Username);

            if (user != null)
            {
                bool isPasswordValid = false;

                // 1. Controleer of het wachtwoord al in de database staat als een BCrypt hash
                if (user.Password != null && (user.Password.StartsWith("$2a$") || user.Password.StartsWith("$2b$") || user.Password.StartsWith("$2y$")))
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.Password);
                }
                // 2. Het is nog een oud, plat-tekst wachtwoord
                else if (user.Password == model.Password)
                {
                    isPasswordValid = true;

                    // Zet het oude wachtwoord direct en geruisloos om naar een veilige hash!
                    user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    _context.SaveChanges();
                }

                // 3. Als het wachtwoord klopt (via hash óf platte tekst), log de gebruiker in
                if (isPasswordValid)
                {
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.GebruikerID.ToString()),
                new Claim(ClaimTypes.Role, user.Rol)
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError(string.Empty, "Onjuiste gebruikersnaam of wachtwoord.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Inloggen", "Account");
        }
    }
}