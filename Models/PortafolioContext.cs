using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using TuProyecto.Models;

namespace Portafolio.Models
{
    public class PortafolioContext : DbContext
    {
        public PortafolioContext(DbContextOptions<PortafolioContext> options)
            : base(options)
        {
        }
        public DbSet<Proyecto> Proyectos { get; set; }
        public DbSet<Visita> Visitas { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
    }
}

