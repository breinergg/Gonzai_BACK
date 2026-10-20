using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

public static class FormatosComprobante
{
    public const string Termico = "termico";
    public const string MediaCarta = "media-carta";

    public static string Normalizar(string? formato)
    {
        var valor = formato?.Trim().ToLowerInvariant();
        return valor switch
        {
            null or "" => Termico,
            Termico => Termico,
            MediaCarta or "mediacarta" or "media_carta" => MediaCarta,
            _ => throw new InvalidOperationException("Formato inválido. Use 'termico' o 'media-carta'.")
        };
    }
}

public static class TiposComprobante
{
    public const string Venta = "Venta";
    public const string Abono = "Abono";
}

/// <summary>Enlace público y temporal para abrir un comprobante sin iniciar sesión.</summary>
[Table("ComprobanteEnlace")]
public class ComprobanteEnlace
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(80)]
    public string Token { get; set; } = string.Empty;
    [Required]
    [MaxLength(20)]
    public string Tipo { get; set; } = TiposComprobante.Venta;
    public int? VentaClienteId { get; set; }
    public int? PagoClienteId { get; set; }
    [Required]
    [MaxLength(20)]
    public string Formato { get; set; } = FormatosComprobante.Termico;
    public DateTime ExpiraEn { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(VentaClienteId))]
    public VentaCliente? VentaCliente { get; set; }
    [ForeignKey(nameof(PagoClienteId))]
    public PagoCliente? PagoCliente { get; set; }
}
