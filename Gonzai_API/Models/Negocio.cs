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
    /// <summary>A este correo llegan las respuestas del cliente.</summary>
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

    /// <summary>
    /// Correo propio de la empresa para enviar comprobantes (cifrado con Data Protection). Si
    /// está presente, se usa en vez de la cuenta compartida de la plataforma: los envíos salen
    /// a nombre de este correo y cuentan para su propio límite diario, no el de GonzAI.
    /// </summary>
    [MaxLength(100)]
    public string? SmtpHost { get; set; }
    public int? SmtpPuerto { get; set; }
    /// <summary>starttls, ssl o none.</summary>
    [MaxLength(20)]
    public string? SmtpSeguridad { get; set; }
    [MaxLength(100)]
    public string? SmtpUsuario { get; set; }
    public string? SmtpPasswordCifrada { get; set; }
    public DateTime? SmtpFecha { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
}
