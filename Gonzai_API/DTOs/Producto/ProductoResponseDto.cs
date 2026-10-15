namespace Gonzai_API.DTOs.Producto;

public class ProductoResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public string? Marca { get; set; }
    public decimal? PrecioVenta { get; set; }
    /// <summary>Suma del stock de todas las variantes.</summary>
    public int StockTotal { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<ProductoVarianteResponseDto> Variantes { get; set; } = [];
}
