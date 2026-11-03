using Microsoft.AspNetCore.Mvc;
using Portafolio.Models;
using TuProyecto.Models;

namespace Portafolio.Controllers
{
    public class AuthController : Controller
    {
        private readonly PortafolioContext _context;

        public AuthController(PortafolioContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.Remove(nameof(model.Password));
                model.Password = string.Empty;

                return View(model);
            }

            string email = model.Email.Trim();

            var user = _context.Usuarios
                .FirstOrDefault(u => u.Email == email);

            if (user == null ||
                !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash) ||
                user.Rol != "Admin")
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Credenciales inválidas."
                );

                ModelState.Remove(nameof(model.Password));
                model.Password = string.Empty;

                return View(model);
            }

            HttpContext.Session.Clear();

            HttpContext.Session.SetString("UserRole", user.Rol);
            HttpContext.Session.SetString("UserEmail", user.Email);

            return RedirectToAction("Index", "Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(nameof(Login));
        }
    }
}