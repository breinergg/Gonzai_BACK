using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

/// <summary>Datos de la tienda que salen en los comprobantes. Hay una sola fila.</summary>
[Table("Negocio")]
public class Negocio
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(100)]
    public string NombreComercial { get; set; } = "GonzAI";
    [MaxLength(20)]
    public string? Telefono { get; set; }
    [MaxLength(80)]
    public string? Ciudad { get; set; }
    /// <summary>A este correo llegan las respuestas del cliente. El envío sale del correo de la plataforma.</summary>
    [MaxLength(100)]
    public string? CorreoRespuesta { get; set; }
    [MaxLength(200)]
    public string Mensaje { get; set; } = "Gracias por tu compra.";
    [Required]
    [MaxLength(20)]
    public string FormatoPredeterminado { get; set; } = FormatosComprobante.Termico;
    public byte[]? Logo { get; set; }

    /// <summary>
    /// Clave de Gemini propia de esta empresa, cifrada con Data Protection. Si está presente, el
    /// asistente de IA la usa en vez de la clave compartida de la plataforma: el consumo (y el
    /// plan gratuito de Google) corre por cuenta de la propia empresa, no de GonzAI.
    /// </summary>
    public string? GeminiApiKeyCifrada { get; set; }
    public DateTime? GeminiApiKeyFecha { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
}
