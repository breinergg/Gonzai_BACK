using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

[Table("MovimientoInventario")]
public class MovimientoInventario
{
    [Key]
    public int Id { get; set; }
    public int ProductoId { get; set; }
    [Required]
    [MaxLength(20)]
    public string TipoMovimiento { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [MaxLength(150)]
    public string? Descripcion { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(ProductoId))]
    public Producto Producto { get; set; } = null!;
}
