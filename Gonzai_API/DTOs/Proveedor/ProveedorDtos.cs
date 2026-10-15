using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.Proveedor;

public class ProveedorCreateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(30, ErrorMessage = "El documento no puede superar 30 caracteres.")]
    public string? Documento { get; set; }

    [MaxLength(20, ErrorMessage = "El teléfono no puede superar 20 caracteres.")]
    public string? Telefono { get; set; }

    [MaxLength(150, ErrorMessage = "La dirección no puede superar 150 caracteres.")]
    public string? Direccion { get; set; }
}

public class ProveedorUpdateDto : ProveedorCreateDto
{
    public bool Activo { get; set; } = true;
}

public class ProveedorResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
