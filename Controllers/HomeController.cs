using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Portafolio.Models;
using TuProyecto.Models;

namespace Portafolio.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PortafolioContext _context;
        private readonly IWebHostEnvironment _env;

        public HomeController(ILogger<HomeController> logger, PortafolioContext context, IWebHostEnvironment env)
        {
            _logger = logger;
            _context = context;
            _env = env;
        }


        public IActionResult Index()
        {
            RegistrarVisita("Home/Index");
            var proyecto = _context.Proyectos
            .Where(p => p.Publicado)
            .OrderByDescending(p => p.FechaPublicacion)
            .FirstOrDefault();
            return View(proyecto);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            RegistrarVisita("Home/About");
            return View();
        }

        public IActionResult Projects()
        {
            RegistrarVisita("Home/Projects");

            var proyectos = _context.Proyectos
                .Where(p => p.Publicado)
                .OrderByDescending(p => p.FechaPublicacion)
                .ToList();

            return View(proyectos);
        }

        public IActionResult Contact()
        {
            RegistrarVisita("Home/Contact");
            return View();
        }


        private void RegistrarVisita(string pagina)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ipEnmascarada = EnmascararIp(ip);
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var visita = new Visita
            {
                Ip = ipEnmascarada,
                Fecha = DateTime.Now,
                Pagina = pagina,
                UserAgent = userAgent
            };

            _context.Visitas.Add(visita);
            _context.SaveChanges();
        }

        private string EnmascararIp(string? ip)
        {
            if (string.IsNullOrEmpty(ip)) return "0.0.0.0";
            var partes = ip.Split('.');
            return partes.Length == 4 ? $"{partes[0]}.{partes[1]}.*.*" : ip;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult DescargarArchivo(int id)
        {
            var proyecto = _context.Proyectos.Find(id);
            if (proyecto == null || string.IsNullOrEmpty(proyecto.ArchivoRuta))
                return NotFound();

            string rutaCompleta = Path.Combine(_env.WebRootPath, proyecto.ArchivoRuta.TrimStart('/'));

            if (!System.IO.File.Exists(rutaCompleta))
                return NotFound();

            string contentType = "application/octet-stream"; // genérico para forzar descarga
            string nombreDescarga = Path.GetFileName(proyecto.ArchivoRuta);

            return File(System.IO.File.ReadAllBytes(rutaCompleta), contentType, nombreDescarga);
        }

    }
}
