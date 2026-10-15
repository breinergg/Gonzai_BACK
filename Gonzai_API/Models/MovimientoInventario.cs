using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

[Table("MovimientoInventario")]
public class MovimientoInventario
{
    [Key]
    public int Id { get; set; }
    public int ProductoVarianteId { get; set; }
    public int ExistenciaLoteId { get; set; }
    [Required]
    [MaxLength(20)]
    public string TipoMovimiento { get; set; } = string.Empty;
    [Required]
    [MaxLength(30)]
    public string Motivo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostoUnitario { get; set; }
    public int StockResultante { get; set; }
    public int? CompraProveedorId { get; set; }
    public int? VentaClienteId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [MaxLength(150)]
    public string? Descripcion { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ProductoVarianteId))]
    public ProductoVariante ProductoVariante { get; set; } = null!;
    [ForeignKey(nameof(ExistenciaLoteId))]
    public ExistenciaLote ExistenciaLote { get; set; } = null!;
    [ForeignKey(nameof(CompraProveedorId))]
    public CompraProveedor? CompraProveedor { get; set; }
    [ForeignKey(nameof(VentaClienteId))]
    public VentaCliente? VentaCliente { get; set; }
    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;
}
