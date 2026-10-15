using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.Producto;

public class ProductoUpdateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public int? CategoriaId { get; set; }

    [MaxLength(50, ErrorMessage = "La marca no puede superar 50 caracteres.")]
    public string? Marca { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor o igual a 0.")]
    public decimal? PrecioVenta { get; set; }

    public bool Activo { get; set; }
}
