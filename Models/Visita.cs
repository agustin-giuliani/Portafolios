using System;

namespace TuProyecto.Models
{
    public class Visita
    {
        public int Id { get; set; }

        public string? Ip { get; set; }  // Coincide con la columna "Ip" en SQL

        public DateTime Fecha { get; set; } = DateTime.Now;

        public string Pagina { get; set; } = ""; // Coincide con la columna "Pagina" en SQL

        public string? UserAgent { get; set; }   // Coincide con la columna "UserAgent" en SQL
    }
}