using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.Producto;

public class ProductoCreateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public int? CategoriaId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El precio de compra debe ser mayor o igual a 0.")]
    public decimal? PrecioCompra { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor o igual a 0.")]
    public decimal? PrecioVenta { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int StockActual { get; set; }
}
