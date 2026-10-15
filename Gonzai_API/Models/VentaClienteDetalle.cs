using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

[Table("VentaClienteDetalle")]
public class VentaClienteDetalle
{
    [Key]
    public int Id { get; set; }
    public int VentaClienteId { get; set; }
    public int ProductoVarianteId { get; set; }
    /// <summary>Lote (compra) elegido por el cliente. Null = el sistema aplicó FIFO.</summary>
    public int? CompraProveedorSolicitadaId { get; set; }
    public int Cantidad { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecioVentaUnitario { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostoTotal { get; set; }

    [ForeignKey(nameof(VentaClienteId))]
    public VentaCliente VentaCliente { get; set; } = null!;
    [ForeignKey(nameof(ProductoVarianteId))]
    public ProductoVariante ProductoVariante { get; set; } = null!;
    [ForeignKey(nameof(CompraProveedorSolicitadaId))]
    public CompraProveedor? CompraProveedorSolicitada { get; set; }

    public ICollection<VentaDetalleExistencia> Consumos { get; set; } = [];
}
