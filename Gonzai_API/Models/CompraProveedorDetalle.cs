using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

[Table("CompraProveedorDetalle")]
public class CompraProveedorDetalle
{
    [Key]
    public int Id { get; set; }
    public int CompraProveedorId { get; set; }
    public int ProductoVarianteId { get; set; }
    public int Cantidad { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostoUnitario { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    [ForeignKey(nameof(CompraProveedorId))]
    public CompraProveedor CompraProveedor { get; set; } = null!;
    [ForeignKey(nameof(ProductoVarianteId))]
    public ProductoVariante ProductoVariante { get; set; } = null!;

    public ExistenciaLote? Existencia { get; set; }
}
