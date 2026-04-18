using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TuProyecto.Models
{
    public class Proyecto
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string? Url { get; set; }
        public string Tecnologias { get; set; }
        public bool Publicado { get; set; }
        public DateTime FechaPublicacion { get; set; } = DateTime.Now;

        // Nueva propiedad para la ruta del archivo
        [BindNever] // Evita que se intente asignar desde el form
        public string? ArchivoRuta { get; set; }

        [NotMapped] // Esto evita que Entity Framework intente guardarlo en la base
        public IFormFile? ArchivoProyecto { get; set; }
    }

}
