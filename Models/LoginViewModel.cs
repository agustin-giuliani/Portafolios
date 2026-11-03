using System.ComponentModel.DataAnnotations;

namespace TuProyecto.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Password { get; set; } = string.Empty;
    }
}