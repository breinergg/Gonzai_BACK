namespace Gonzai_API.DTOs.Producto;

public class ProductoResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public decimal? PrecioCompra { get; set; }
    public decimal? PrecioVenta { get; set; }
    public int StockActual { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
