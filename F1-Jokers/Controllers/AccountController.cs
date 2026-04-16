using Microsoft.AspNetCore.Mvc;
using F1Jokers.Models; 

namespace F1Jokers.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Inloggen()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Inloggen(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Hier komt later de echte check
                return RedirectToAction("Index", "Voorspelling");
            }

            return View(model);
        }
    }
}