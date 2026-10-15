using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.MovimientoInventario;

/// <summary>
/// Ajuste manual de inventario (conteo físico, pérdidas, daños, mercancía sin factura).
/// Las entradas por compra y las salidas por venta las genera el sistema automáticamente.
/// </summary>
public class MovimientoInventarioCreateDto
{
    [Required(ErrorMessage = "La variante del producto es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La variante del producto es obligatoria.")]
    public int ProductoVarianteId { get; set; }

    [Required(ErrorMessage = "El tipo de movimiento es obligatorio.")]
    [RegularExpression("^(entrada|salida)$", ErrorMessage = "El tipo de movimiento solo puede ser 'entrada' o 'salida'.")]
    public string TipoMovimiento { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }

    /// <summary>Solo para entradas. Si no se envía, se usa el último costo de compra de la variante.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "El costo unitario debe ser mayor o igual a 0.")]
    public decimal? CostoUnitario { get; set; }

    /// <summary>Solo para salidas: descontar de este lote (compra). Si no se envía, FIFO.</summary>
    public int? CompraProveedorId { get; set; }

    /// <summary>Solo para salidas: descontar de una existencia puntual (por ejemplo, inventario inicial).</summary>
    public int? ExistenciaId { get; set; }

    [Required(ErrorMessage = "La descripción del ajuste es obligatoria.")]
    [MaxLength(150, ErrorMessage = "La descripción no puede superar 150 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;
}
