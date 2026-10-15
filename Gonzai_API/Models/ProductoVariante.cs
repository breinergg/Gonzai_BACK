using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

/// <summary>
/// Unidad que realmente se compra, se cuenta y se vende (SKU): "Camiseta Nike Negra XL".
/// Talla y Color en null representan la variante única de un producto sin variantes.
/// </summary>
[Table("ProductoVariante")]
public class ProductoVariante
{
    [Key]
    public int Id { get; set; }
    public int ProductoId { get; set; }
    [MaxLength(20)]
    public string? Talla { get; set; }
    [MaxLength(30)]
    public string? Color { get; set; }
    [MaxLength(50)]
    public string? Sku { get; set; }
    /// <summary>Si es null se usa el precio de venta del producto.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? PrecioVenta { get; set; }
    /// <summary>Último costo de compra, solo como referencia: el costo real de cada venta sale del lote.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? PrecioCompra { get; set; }
    public int StockActual { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }

    [ForeignKey(nameof(ProductoId))]
    public Producto Producto { get; set; } = null!;

    public ICollection<ExistenciaLote> Existencias { get; set; } = [];
    public ICollection<MovimientoInventario> MovimientosInventario { get; set; } = [];

    /// <summary>Requiere que <see cref="Producto"/> esté cargado.</summary>
    [NotMapped]
    public string NombreCompleto => Componer(Producto?.Nombre, Color, Talla);

    public static string Componer(string? producto, string? color, string? talla) =>
        string.Join(" ", new[] { producto, color, talla }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
