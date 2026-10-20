using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.Comprobante;

public class EnviarComprobanteDto
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [MaxLength(100)]
    public string Correo { get; set; } = string.Empty;

    /// <summary>termico o media-carta. Si no se envía, se usa el formato del negocio.</summary>
    public string? Formato { get; set; }
}

public class CompartirComprobanteDto
{
    public string Url { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    /// <summary>Null si el cliente no tiene un teléfono usable.</summary>
    public string? WhatsappUrl { get; set; }
    public DateTime ExpiraEn { get; set; }
    public string Formato { get; set; } = string.Empty;
}
