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

        private string? ObtenerRutaArchivoSeguro(string? archivoRuta)
        {
            if (string.IsNullOrWhiteSpace(archivoRuta))
                return null;

            string rutaNormalizada = archivoRuta.Replace('\\', '/');

            const string prefijo = "/uploads/";

            if (!rutaNormalizada.StartsWith(
                prefijo,
                StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string nombreArchivo = rutaNormalizada.Substring(prefijo.Length);

            // No permitir subdirectorios ni rutas relativas.
            if (string.IsNullOrWhiteSpace(nombreArchivo) ||
                nombreArchivo != Path.GetFileName(nombreArchivo))
            {
                return null;
            }

            string carpetaUploads = Path.GetFullPath(
                Path.Combine(_env.WebRootPath, "uploads"));

            if (!Directory.Exists(carpetaUploads))
                return null;

            string rutaCompleta = Path.GetFullPath(
                Path.Combine(carpetaUploads, nombreArchivo));

            string prefijoSeguro = carpetaUploads +
                Path.DirectorySeparatorChar;

            if (!rutaCompleta.StartsWith(
                prefijoSeguro,
                StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return rutaCompleta;
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
                    var extensionArchivo = Path.GetExtension(proyecto.ArchivoProyecto.FileName).ToLowerInvariant();

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
                    string uniqueFileName = $"{Guid.NewGuid():N}{extensionArchivo}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(
                        filePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None))
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
        public async Task<IActionResult> Edit(int id, Proyecto proyecto, IFormFile? ArchivoProyecto)
        {
            if (id != proyecto.Id)
                return NotFound();

            var proyectoDb = await _context.Proyectos.FindAsync(id);

            if (proyectoDb == null)
                return NotFound();

            // Evitamos validar propiedades que no se editan desde este formulario.
            ModelState.Remove(nameof(Proyecto.ArchivoRuta));
            ModelState.Remove(nameof(Proyecto.ArchivoProyecto));

            // Si el modelo no es válido, no guardamos los cambios.
            if (!ModelState.IsValid)
                return View(proyecto);

            string? nuevaRutaArchivo = null;
            string? rutaArchivoAnterior = proyectoDb.ArchivoRuta;

            // Validar el archivo nuevo, si se adjuntó uno.
            if (ArchivoProyecto != null && ArchivoProyecto.Length > 0)
            {
                var extensionesPermitidas = new[]
                {
                    ".pdf", ".zip", ".png", ".jpg",
                    ".jpeg", ".xlsx", ".pbix"
                };

                string extension = Path.GetExtension(ArchivoProyecto.FileName)
                    .ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                {
                    ModelState.AddModelError(
                        "ArchivoProyecto",
                        "Formato no permitido. Solo: PDF, ZIP, PNG, JPG, XLSX, PBIX."
                    );

                    return View(proyecto);
                }

                const long tamanioMaximo = 20 * 1024 * 1024;

                if (ArchivoProyecto.Length > tamanioMaximo)
                {
                    ModelState.AddModelError(
                        "ArchivoProyecto",
                        "El archivo no puede superar los 20 MB."
                    );

                    return View(proyecto);
                }

                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");

                Directory.CreateDirectory(uploadsFolder);

                // Nombre aleatorio. No conservamos el nombre original.
                string uniqueFileName = $"{Guid.NewGuid():N}{extension}";

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                try
                {
                    using (var stream = new FileStream(
                        filePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None))
                    {
                        await ArchivoProyecto.CopyToAsync(stream);
                    }

                    nuevaRutaArchivo = "/uploads/" + uniqueFileName;
                }
                catch
                {
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);

                    throw;
                }
            }

            // Actualizar únicamente los campos permitidos.
            proyectoDb.Titulo = proyecto.Titulo;
            proyectoDb.Descripcion = proyecto.Descripcion;
            proyectoDb.Tecnologias = proyecto.Tecnologias;
            proyectoDb.Publicado = proyecto.Publicado;

            if (nuevaRutaArchivo != null)
            {
                proyectoDb.ArchivoRuta = nuevaRutaArchivo;
                proyectoDb.Url = null;
            }
            else
            {
                proyectoDb.Url = string.IsNullOrWhiteSpace(proyecto.Url)
                    ? null
                    : proyecto.Url.Trim();
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Si falla la base de datos, eliminamos el archivo nuevo
                // para no dejar archivos huérfanos.
                if (nuevaRutaArchivo != null)
                {
                    string nuevoArchivo = Path.Combine(
                        _env.WebRootPath,
                        nuevaRutaArchivo.TrimStart('/').Replace(
                            '/',
                            Path.DirectorySeparatorChar
                        )
                    );

                    if (System.IO.File.Exists(nuevoArchivo))
                        System.IO.File.Delete(nuevoArchivo);
                }

                throw;
            }

            // Si todo salió bien, eliminamos el archivo anterior.
            if (nuevaRutaArchivo != null &&
                !string.IsNullOrWhiteSpace(rutaArchivoAnterior))
            {
                string rutaAnterior = rutaArchivoAnterior.Replace('\\', '/');
                if (nuevaRutaArchivo != null &&
                    !string.IsNullOrWhiteSpace(rutaArchivoAnterior))
                {
                    string? archivoAnterior = ObtenerRutaArchivoSeguro(rutaArchivoAnterior);

                    if (archivoAnterior != null &&
                        System.IO.File.Exists(archivoAnterior))
                    {
                        System.IO.File.Delete(archivoAnterior);
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }



        // GET: /Admin/Delete/5
        public IActionResult Delete(int id)
        {
            var proyecto = _context.Proyectos.Find(id);
            if (proyecto == null) return NotFound();

            return View(proyecto);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proyecto = await _context.Proyectos.FindAsync(id);

            if (proyecto == null)
                return NotFound();

            string? rutaArchivo = ObtenerRutaArchivoSeguro(proyecto.ArchivoRuta);

            _context.Proyectos.Remove(proyecto);
            await _context.SaveChangesAsync();

            if (rutaArchivo != null && System.IO.File.Exists(rutaArchivo))
            {
                System.IO.File.Delete(rutaArchivo);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> DescargarArchivo(int id)
        {
            var proyecto = await _context.Proyectos.FindAsync(id);

            if (proyecto == null || string.IsNullOrWhiteSpace(proyecto.ArchivoRuta))
                return NotFound();

            string? rutaArchivo = ObtenerRutaArchivoSeguro(proyecto.ArchivoRuta);

            if (rutaArchivo == null || !System.IO.File.Exists(rutaArchivo))
                return NotFound();

            string nombreDescarga = Path.GetFileName(rutaArchivo);

            return PhysicalFile(
                rutaArchivo,
                "application/octet-stream",
                nombreDescarga
            );
        }

    }
}

