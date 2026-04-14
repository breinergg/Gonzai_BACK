using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.Usuario;

public class UsuarioUpdateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El rol no puede superar 20 caracteres.")]
    public string Rol { get; set; } = string.Empty;

    public bool Activo { get; set; }
}
