using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.CompraProveedor;

public class CompraProveedorDetalleCreateDto
{
    [Required(ErrorMessage = "La variante del producto es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La variante del producto es obligatoria.")]
    public int ProductoVarianteId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }

    /// <summary>Costo de cada prenda, sin envío.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "El costo unitario debe ser mayor o igual a 0.")]
    public decimal CostoUnitario { get; set; }
}

public class CompraProveedorCreateDto
{
    [Required(ErrorMessage = "El proveedor es obligatorio.")]
    public int ProveedorId { get; set; }

    /// <summary>Nombre libre del lote, por ejemplo "Pedido Nike temporada escolar".</summary>
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string? Nombre { get; set; }

    /// <summary>Fecha de la compra. Si no se envía, se usa la fecha actual.</summary>
    public DateTime? Fecha { get; set; }

    [MaxLength(50, ErrorMessage = "El número de factura no puede superar 50 caracteres.")]
    public string? NumeroFactura { get; set; }

    [MaxLength(150, ErrorMessage = "La descripción no puede superar 150 caracteres.")]
    public string? Descripcion { get; set; }

    /// <summary>Envío del lote. No cambia el costo de las prendas; se descuenta en la rentabilidad.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "El costo de envío debe ser mayor o igual a 0.")]
    public decimal CostoEnvio { get; set; }

    [Required(ErrorMessage = "La compra debe tener al menos un producto.")]
    [MinLength(1, ErrorMessage = "La compra debe tener al menos un producto.")]
    public List<CompraProveedorDetalleCreateDto> Detalles { get; set; } = [];
}

public class CompraProveedorDetalleResponseDto
{
    public int Id { get; set; }
    public int ProductoVarianteId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string VarianteNombre { get; set; } = string.Empty;
    public string? Talla { get; set; }
    public string? Color { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public int? ExistenciaId { get; set; }
    public int? CantidadDisponible { get; set; }
}

public class CompraProveedorResponseDto
{
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string? Nombre { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? NumeroFactura { get; set; }
    public string? Descripcion { get; set; }
    public int TotalUnidades { get; set; }
    public decimal SubtotalProductos { get; set; }
    public decimal CostoEnvio { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public List<CompraProveedorDetalleResponseDto> Detalles { get; set; } = [];
}
