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

    /// <summary>
    /// true si la empresa configuró su propia clave de Gemini. La clave nunca se devuelve:
    /// solo se informa si está configurada o no.
    /// </summary>
    public bool TieneClaveIaPropia { get; set; }
    public DateTime? ClaveIaFecha { get; set; }

    /// <summary>
    /// true si la empresa configuró su propio correo para enviar comprobantes. La contraseña
    /// nunca se devuelve; el usuario sí, para que sepas cuál está activo.
    /// </summary>
    public bool TieneSmtpPropio { get; set; }
    public string? SmtpUsuario { get; set; }
    public DateTime? SmtpFecha { get; set; }
}

public class NegocioGeminiApiKeyDto
{
    [Required(ErrorMessage = "La clave de Gemini es obligatoria.")]
    [MinLength(10, ErrorMessage = "La clave no parece válida.")]
    [MaxLength(200, ErrorMessage = "La clave no puede superar 200 caracteres.")]
    public string ApiKey { get; set; } = string.Empty;
}

public class NegocioSmtpDto
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [MaxLength(100, ErrorMessage = "El correo no puede superar 100 caracteres.")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña de aplicación es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña no parece válida.")]
    [MaxLength(200, ErrorMessage = "La contraseña no puede superar 200 caracteres.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Vacío = Gmail (smtp.gmail.com). Solo hace falta para otro proveedor de correo.</summary>
    [MaxLength(100, ErrorMessage = "El servidor no puede superar 100 caracteres.")]
    public string? Host { get; set; }

    [Range(1, 65535, ErrorMessage = "El puerto no es válido.")]
    public int? Puerto { get; set; }

    /// <summary>starttls (por defecto), ssl o none.</summary>
    [MaxLength(20)]
    public string? Seguridad { get; set; }
}
