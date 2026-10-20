using System.ComponentModel.DataAnnotations;
using Gonzai_API.Models;

namespace Gonzai_API.DTOs.Negocio;

public class NegocioUpdateDto
{
    [Required(ErrorMessage = "El nombre comercial es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string NombreComercial { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "El teléfono no puede superar 20 caracteres.")]
    public string? Telefono { get; set; }

    [MaxLength(80, ErrorMessage = "La ciudad no puede superar 80 caracteres.")]
    public string? Ciudad { get; set; }

    [EmailAddress(ErrorMessage = "El correo de respuesta no es válido.")]
    [MaxLength(100, ErrorMessage = "El correo no puede superar 100 caracteres.")]
    public string? CorreoRespuesta { get; set; }

    [MaxLength(200, ErrorMessage = "El mensaje no puede superar 200 caracteres.")]
    public string? Mensaje { get; set; }

    /// <summary>termico o media-carta.</summary>
    public string? FormatoPredeterminado { get; set; }
}

public class NegocioResponseDto
{
    public int Id { get; set; }
    public string NombreComercial { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Ciudad { get; set; }
    public string? CorreoRespuesta { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string FormatoPredeterminado { get; set; } = FormatosComprobante.Termico;
    public bool TieneLogo { get; set; }
}
