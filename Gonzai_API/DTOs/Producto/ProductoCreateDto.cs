using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.Producto;

public class ProductoCreateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public int? CategoriaId { get; set; }

    [MaxLength(50, ErrorMessage = "La marca no puede superar 50 caracteres.")]
    public string? Marca { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor o igual a 0.")]
    public decimal? PrecioVenta { get; set; }

    /// <summary>
    /// Tallas/colores del producto. Si se deja vacío se crea una única variante por defecto
    /// con <see cref="StockInicial"/> y <see cref="CostoInicial"/>.
    /// </summary>
    public List<ProductoVarianteCreateDto> Variantes { get; set; } = [];

    /// <summary>Solo aplica cuando no se envían variantes.</summary>
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int StockInicial { get; set; }

    /// <summary>Solo aplica cuando no se envían variantes.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "El costo debe ser mayor o igual a 0.")]
    public decimal? CostoInicial { get; set; }
}
