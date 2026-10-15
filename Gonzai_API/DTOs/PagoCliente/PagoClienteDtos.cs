using System.ComponentModel.DataAnnotations;
using Gonzai_API.Models;

namespace Gonzai_API.DTOs.PagoCliente;

public class PagoClienteCreateDto
{
    [Required(ErrorMessage = "La venta es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La venta es obligatoria.")]
    public int VentaClienteId { get; set; }

    /// <summary>Debe coincidir con el cliente de la venta.</summary>
    [Required(ErrorMessage = "El cliente es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El cliente es obligatorio.")]
    public int ClienteId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0.")]
    public decimal Monto { get; set; }

    [Required(ErrorMessage = "El método de pago es obligatorio.")]
    [RegularExpression(MetodosPago.Patron,
        ErrorMessage = "Método de pago inválido. Use: efectivo, transferencia, tarjeta, nequi, daviplata u otro.")]
    public string MetodoPago { get; set; } = string.Empty;

    /// <summary>Fecha del pago. Si no se envía, se usa la fecha actual.</summary>
    public DateTime? Fecha { get; set; }

    [MaxLength(150, ErrorMessage = "La descripción no puede superar 150 caracteres.")]
    public string? Descripcion { get; set; }
}

public class PagoClienteResponseDto
{
    public int Id { get; set; }
    public int VentaClienteId { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? Descripcion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public decimal SaldoPendienteVenta { get; set; }
}
