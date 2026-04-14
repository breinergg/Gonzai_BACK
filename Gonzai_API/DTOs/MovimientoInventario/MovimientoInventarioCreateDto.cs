using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.MovimientoInventario;

public class MovimientoInventarioCreateDto
{
    [Required(ErrorMessage = "El producto es obligatorio.")]
    public int ProductoId { get; set; }

    [Required(ErrorMessage = "El tipo de movimiento es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El tipo de movimiento no puede superar 20 caracteres.")]
    public string TipoMovimiento { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }

    [MaxLength(150, ErrorMessage = "La descripción no puede superar 150 caracteres.")]
    public string? Descripcion { get; set; }
}
