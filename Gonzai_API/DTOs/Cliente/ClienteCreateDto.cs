using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.Cliente;

public class ClienteCreateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "El teléfono no puede superar 20 caracteres.")]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [MaxLength(100, ErrorMessage = "El correo no puede superar 100 caracteres.")]
    public string? Email { get; set; }

    [MaxLength(150, ErrorMessage = "La dirección no puede superar 150 caracteres.")]
    public string? Direccion { get; set; }
}
