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
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Usuarios
                .FirstOrDefault(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ViewBag.Error = "Credenciales inválidas";
                return View();
            }

            HttpContext.Session.SetString("UserRole", user.Rol);
            HttpContext.Session.SetString("UserEmail", user.Email);

            return RedirectToAction("Index", "Admin");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }

}
