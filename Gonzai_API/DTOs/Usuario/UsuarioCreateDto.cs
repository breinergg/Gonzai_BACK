using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.Usuario;

public class UsuarioCreateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El email no puede superar 100 caracteres.")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El rol no puede superar 20 caracteres.")]
    public string Rol { get; set; } = string.Empty;
}
