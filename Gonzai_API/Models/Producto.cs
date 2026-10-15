using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

/// <summary>
/// Modelo de producto ("Camiseta Nike Dri-Fit"). El stock y los costos viven en sus
/// variantes (talla/color); un producto sin tallas tiene una única variante por defecto.
/// </summary>
[Table("Producto")]
public class Producto
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;
    public int? CategoriaId { get; set; }
    [MaxLength(50)]
    public string? Marca { get; set; }
    /// <summary>Precio de venta sugerido para todas las variantes que no definan uno propio.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? PrecioVenta { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }

    [ForeignKey(nameof(CategoriaId))]
    public Categoria? Categoria { get; set; }

    public ICollection<ProductoVariante> Variantes { get; set; } = [];
}
