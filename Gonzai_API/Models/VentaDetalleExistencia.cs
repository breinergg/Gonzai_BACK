using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

/// <summary>De qué existencia salió cada unidad de una línea de venta y con qué costo.</summary>
[Table("VentaDetalleExistencia")]
public class VentaDetalleExistencia
{
    [Key]
    public int Id { get; set; }
    public int VentaClienteDetalleId { get; set; }
    public int ExistenciaLoteId { get; set; }
    public int Cantidad { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostoUnitario { get; set; }

    [ForeignKey(nameof(VentaClienteDetalleId))]
    public VentaClienteDetalle VentaClienteDetalle { get; set; } = null!;
    [ForeignKey(nameof(ExistenciaLoteId))]
    public ExistenciaLote ExistenciaLote { get; set; } = null!;
}
