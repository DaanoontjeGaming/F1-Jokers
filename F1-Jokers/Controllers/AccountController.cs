using F1Jokers.Data;
using F1Jokers.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
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
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(3)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
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

        // ==========================================
        // PROFIEL BEHEREN
        // ==========================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profiel()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Inloggen");

            int userId = int.Parse(userIdClaim.Value);
            var gebruiker = await _context.Gebruikers.FindAsync(userId);

            if (gebruiker == null) return NotFound();

            var model = new ProfielViewModel
            {
                Username = gebruiker.Username,
                Email = gebruiker.Email
            };

            // Bereken de actuele stand in de poule voor het dashboard
            var alleDeelnemers = await _context.Gebruikers
                .Where(g => g.Rol == "Deelnemer")
                .OrderByDescending(g => g.GebruikerPoints)
                .ToListAsync();

            int positie = alleDeelnemers.FindIndex(g => g.GebruikerID == userId) + 1;

            ViewBag.TotalePunten = gebruiker.GebruikerPoints ?? 0;
            ViewBag.Champagne = gebruiker.Champagne;
            ViewBag.HuidigeStand = positie > 0 ? positie.ToString() : "-";

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profiel(ProfielViewModel model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdClaim.Value);
            var gebruiker = await _context.Gebruikers.FindAsync(userId);

            if (gebruiker == null) return NotFound();

            var alleDeelnemers = await _context.Gebruikers
                .Where(g => g.Rol == "Deelnemer")
                .OrderByDescending(g => g.GebruikerPoints)
                .ToListAsync();

            int positie = alleDeelnemers.FindIndex(g => g.GebruikerID == userId) + 1;

            ViewBag.TotalePunten = gebruiker.GebruikerPoints ?? 0;
            ViewBag.Champagne = gebruiker.Champagne;
            ViewBag.HuidigeStand = positie > 0 ? positie.ToString() : "-";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool isGewijzigd = false;

            // 1. Gebruikersnaam wijzigen
            if (gebruiker.Username != model.Username)
            {
                bool naamBestaatAl = await _context.Gebruikers.AnyAsync(g => g.Username == model.Username && g.GebruikerID != userId);
                if (naamBestaatAl)
                {
                    ModelState.AddModelError("Username", "Deze gebruikersnaam is al in gebruik. Kies een andere.");
                    return View(model);
                }

                gebruiker.Username = model.Username;
                isGewijzigd = true;
            }

            // 2. E-mailadres wijzigen
            if (gebruiker.Email != model.Email)
            {
                bool emailBestaatAl = await _context.Gebruikers.AnyAsync(g => g.Email == model.Email && g.GebruikerID != userId);
                if (emailBestaatAl)
                {
                    ModelState.AddModelError("Email", "Dit e-mailadres is al gekoppeld aan een ander account.");
                    return View(model);
                }

                gebruiker.Email = model.Email;
                isGewijzigd = true;
            }

            // 3. Wachtwoord wijzigen met BCrypt-validatie en hashing (Work Factor 11)
            if (!string.IsNullOrEmpty(model.NieuwWachtwoord))
            {
                if (string.IsNullOrEmpty(model.HuidigWachtwoord) || !BCrypt.Net.BCrypt.Verify(model.HuidigWachtwoord, gebruiker.Password))
                {
                    ModelState.AddModelError("HuidigWachtwoord", "Het huidige wachtwoord is onjuist ingevuld.");
                    return View(model);
                }

                gebruiker.Password = BCrypt.Net.BCrypt.HashPassword(model.NieuwWachtwoord, 11);
                isGewijzigd = true;
            }

            if (isGewijzigd)
            {
                _context.Update(gebruiker);
                await _context.SaveChangesAsync();

                // Vernieuw de authenticatie-cookie direct als de naam is veranderd
                if (gebruiker.Username != User.Identity.Name)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, gebruiker.Username),
                        new Claim(ClaimTypes.NameIdentifier, gebruiker.GebruikerID.ToString()),
                        new Claim(ClaimTypes.Role, gebruiker.Rol)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(3)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                }

                TempData["SuccessMessage"] = "Je instellingen zijn succesvol opgeslagen!";
            }

            model.HuidigWachtwoord = string.Empty;
            model.NieuwWachtwoord = string.Empty;
            model.BevestigNieuwWachtwoord = string.Empty;

            return View(model);
        }
    }
}