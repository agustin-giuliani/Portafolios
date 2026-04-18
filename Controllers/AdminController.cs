using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portafolio.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using TuProyecto.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IO;
using System.Threading.Tasks;

namespace Portafolio.Controllers
{
    public class AdminController : Controller
    {
        private readonly PortafolioContext _context;
        private readonly IWebHostEnvironment _env;
        public AdminController(PortafolioContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        //  PROTECCIÓN DEL ADMIN
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var rol = HttpContext.Session.GetString("UserRole");

            if (rol != "Admin")
            {
                context.Result = RedirectToAction("Login", "Auth");
            }

            base.OnActionExecuting(context);
        }
        // GET: /Admin
        public IActionResult Index()
        {
            ViewBag.TotalVisitas = _context.Visitas.Count();
            List<Proyecto> proyectos = _context.Proyectos.OrderByDescending(p => p.FechaPublicacion).ToList();

            return View(proyectos);
        }

        // GET: /Admin/Create
        public IActionResult Create()
        {
            return View();
        }

        // GET: /Admin/Visitas
        public IActionResult Visitas()
        {
            var visitas = _context.Visitas
                .OrderByDescending(v => v.Fecha)
                .ToList();

            return View(visitas);
        }

        // POST: /Admin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Proyecto proyecto)
        {
            ModelState.Remove("ArchivoRuta");

            // Verificar que tenga al menos URL o Archivo
            if (string.IsNullOrWhiteSpace(proyecto.Url) && (proyecto.ArchivoProyecto == null || proyecto.ArchivoProyecto.Length == 0))
            {
                ModelState.AddModelError("", "Debe ingresar una URL o subir un archivo.");
                return View(proyecto);
            }

            if (ModelState.IsValid)
            {
                if (proyecto.ArchivoProyecto != null && proyecto.ArchivoProyecto.Length > 0)
                {
                    // Extensiones permitidas
                    var extensionesPermitidas = new[] { ".pdf", ".zip", ".png", ".jpg", ".jpeg", ".xlsx", ".pbix" };
                    var extensionArchivo = Path.GetExtension(proyecto.ArchivoProyecto.FileName).ToLower();

                    if (!extensionesPermitidas.Contains(extensionArchivo))
                    {
                        ModelState.AddModelError("ArchivoProyecto", "Formato no permitido. Solo: PDF, ZIP, PNG, JPG, XLSX, PBIX");
                        return View(proyecto);
                    }

                    if (proyecto.ArchivoProyecto.Length > 20 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ArchivoProyecto", "El archivo no puede superar los 20 MB.");
                        return View(proyecto);
                    }

                    // Carpeta de destino
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    // Nombre único
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + proyecto.ArchivoProyecto.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        proyecto.ArchivoProyecto.CopyTo(fileStream);
                    }

                    // Guardar la ruta en BD
                    proyecto.ArchivoRuta = "/uploads/" + uniqueFileName;
                }
                if (string.IsNullOrWhiteSpace(proyecto.Url))
                {
                    proyecto.Url = null;
                }
                _context.Proyectos.Add(proyecto);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            return View(proyecto);
        }


        // GET: /Admin/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var proyecto = await _context.Proyectos.FindAsync(id);
            if (proyecto == null)
            {
                return NotFound();
            }

            return View(proyecto);
        }


        // POST: Admin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Proyecto proyecto, IFormFile ArchivoProyecto)
        {
            if (id != proyecto.Id)
                return NotFound();

            var proyectoDb = await _context.Proyectos.FindAsync(id);
            if (proyectoDb == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var e in errors)
                {
                    Console.WriteLine(e.ErrorMessage);
                }
            }
            else
            {
                proyectoDb.Titulo = proyecto.Titulo;
                proyectoDb.Descripcion = proyecto.Descripcion;
                proyectoDb.Tecnologias = proyecto.Tecnologias;
                proyectoDb.Publicado = proyecto.Publicado;

                // ✅ Si se sube un archivo nuevo
                if (ArchivoProyecto != null && ArchivoProyecto.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + ArchivoProyecto.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ArchivoProyecto.CopyToAsync(stream);
                    }

                    proyectoDb.ArchivoRuta = "/uploads/" + uniqueFileName;
                    proyectoDb.Url = null; // si hay archivo, limpiamos URL
                }
                else
                {
                    // ✅ Si no hay archivo, guardamos la URL que venga del form
                    if (string.IsNullOrWhiteSpace(proyecto.Url))
                    {
                        proyectoDb.Url = null; // si el admin borra la URL, la limpiamos
                    }
                    else
                    {
                        proyectoDb.Url = proyecto.Url;
                    }
                }
                _context.Update(proyectoDb);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(proyecto);
        }




        // GET: /Admin/Delete/5
        public IActionResult Delete(int id)
        {
            var proyecto = _context.Proyectos.Find(id);
            if (proyecto == null) return NotFound();

            return View(proyecto);
        }

        // POST: /Admin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var proyecto = _context.Proyectos.Find(id);
            if (proyecto != null)
            {
                // Eliminar archivo único
                if (!string.IsNullOrEmpty(proyecto.ArchivoRuta))
                {
                    string rutaArchivo = Path.Combine(_env.WebRootPath, proyecto.ArchivoRuta.TrimStart('/'));
                    if (System.IO.File.Exists(rutaArchivo))
                    {
                        System.IO.File.Delete(rutaArchivo);
                    }
                }

                _context.Proyectos.Remove(proyecto);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult DescargarArchivo(int id)
        {
            var proyecto = _context.Proyectos.Find(id);
            if (proyecto == null || string.IsNullOrEmpty(proyecto.ArchivoRuta))
                return NotFound();

            string rutaCompleta = Path.Combine(_env.WebRootPath, proyecto.ArchivoRuta.TrimStart('/'));

            if (!System.IO.File.Exists(rutaCompleta))
                return NotFound();

            string contentType = "application/octet-stream"; // fuerza descarga
            string nombreDescarga = Path.GetFileName(proyecto.ArchivoRuta);

            return File(System.IO.File.ReadAllBytes(rutaCompleta), contentType, nombreDescarga);
        }

    }
}

