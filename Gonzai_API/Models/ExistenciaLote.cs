using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

/// <summary>
/// Unidades de una variante que entraron juntas con un mismo costo. Una compra (el "lote" del
/// negocio) genera una existencia por cada línea; el inventario inicial y los ajustes de entrada
/// generan existencias sin compra. Las ventas descuentan de aquí para conocer su costo real.
/// </summary>
[Table("ExistenciaLote")]
public class ExistenciaLote
{
    [Key]
    public int Id { get; set; }
    public int ProductoVarianteId { get; set; }
    public int? CompraProveedorDetalleId { get; set; }
    [Required]
    [MaxLength(30)]
    public string Origen { get; set; } = string.Empty;
    public int CantidadInicial { get; set; }
    public int CantidadDisponible { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostoUnitario { get; set; }
    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }

    [ForeignKey(nameof(ProductoVarianteId))]
    public ProductoVariante ProductoVariante { get; set; } = null!;
    [ForeignKey(nameof(CompraProveedorDetalleId))]
    public CompraProveedorDetalle? CompraProveedorDetalle { get; set; }
}
