using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.Cliente;

public class ClienteUpdateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "El teléfono no puede superar 20 caracteres.")]
    public string? Telefono { get; set; }

    [MaxLength(150, ErrorMessage = "La dirección no puede superar 150 caracteres.")]
    public string? Direccion { get; set; }

    public bool Activo { get; set; }
}
