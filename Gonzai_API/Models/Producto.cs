using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

[Table("Producto")]
public class Producto
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;
    public int? CategoriaId { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? PrecioCompra { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? PrecioVenta { get; set; }
    public int StockActual { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }

    [ForeignKey(nameof(CategoriaId))]
    public Categoria? Categoria { get; set; }

    public ICollection<MovimientoInventario> MovimientosInventario { get; set; } = [];
}
