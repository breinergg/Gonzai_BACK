using System.ComponentModel.DataAnnotations;
using Gonzai_API.Models;

namespace Gonzai_API.DTOs.VentaCliente;

public class VentaClienteDetalleCreateDto
{
    [Required(ErrorMessage = "La variante del producto es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La variante del producto es obligatoria.")]
    public int ProductoVarianteId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor o igual a 0.")]
    public decimal PrecioVentaUnitario { get; set; }

    /// <summary>
    /// Lote (compra a proveedor) del que el cliente quiere la prenda. Si no se envía,
    /// se descuenta por FIFO: primero lo más antiguo.
    /// </summary>
    public int? CompraProveedorId { get; set; }
}

public class VentaClienteCreateDto
{
    [Required(ErrorMessage = "El cliente es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El cliente es obligatorio.")]
    public int ClienteId { get; set; }

    /// <summary>Fecha de la venta. Si no se envía, se usa la fecha actual.</summary>
    public DateTime? Fecha { get; set; }

    [MaxLength(150, ErrorMessage = "La descripción no puede superar 150 caracteres.")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "La venta debe tener al menos un producto.")]
    [MinLength(1, ErrorMessage = "La venta debe tener al menos un producto.")]
    public List<VentaClienteDetalleCreateDto> Detalles { get; set; } = [];

    /// <summary>Lo que el cliente paga en el momento. 0 = queda debiendo todo.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "El monto pagado no puede ser negativo.")]
    public decimal MontoPagado { get; set; }

    /// <summary>Obligatorio cuando MontoPagado es mayor a 0.</summary>
    [RegularExpression(MetodosPago.Patron,
        ErrorMessage = "Método de pago inválido. Use: efectivo, transferencia, tarjeta, nequi, daviplata u otro.")]
    public string? MetodoPago { get; set; }
}

public class VentaDetalleExistenciaResponseDto
{
    public int ExistenciaId { get; set; }
    /// <summary>Lote (compra) de origen; null si salió de inventario inicial o de un ajuste.</summary>
    public int? CompraProveedorId { get; set; }
    public string? LoteCodigo { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
}

public class VentaClienteDetalleResponseDto
{
    public int Id { get; set; }
    public int ProductoVarianteId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string VarianteNombre { get; set; } = string.Empty;
    public string? Talla { get; set; }
    public string? Color { get; set; }
    public int? CompraProveedorSolicitadaId { get; set; }
    public string? LoteSolicitadoCodigo { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioVentaUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public decimal CostoTotal { get; set; }
    public decimal Ganancia => Subtotal - CostoTotal;
    public List<VentaDetalleExistenciaResponseDto> Consumos { get; set; } = [];
}

public class VentaClientePagoResumenDto
{
    public int Id { get; set; }
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class VentaClienteResponseDto
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? Descripcion { get; set; }
    public decimal Total { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string EstadoPago { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public decimal CostoTotal { get; set; }
    public decimal Ganancia => Total - CostoTotal;
    public List<VentaClienteDetalleResponseDto> Detalles { get; set; } = [];
    public List<VentaClientePagoResumenDto> Pagos { get; set; } = [];
}

public class VentaMensualResumenDto
{
    public string Mes { get; set; } = string.Empty;
    public string MesAnterior { get; set; } = string.Empty;
    public decimal TotalMesActual { get; set; }
    public decimal TotalMesAnterior { get; set; }
    public decimal PorcentajeCambio { get; set; }
    public bool EsAumento { get; set; }
}
