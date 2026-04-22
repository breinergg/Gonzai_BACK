using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.MovimientoCliente;

public class MovimientoClienteCreateDto
{
    [Required(ErrorMessage = "El cliente es obligatorio.")]
    public int ClienteId { get; set; }

    [Required(ErrorMessage = "El tipo de movimiento es obligatorio.")]
    [RegularExpression("^(deuda|abono)$", ErrorMessage = "El tipo de movimiento solo puede ser 'deuda' o 'abono'.")]
    public string TipoMovimiento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El valor es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El valor debe ser mayor a 0.")]
    public decimal Valor { get; set; }

    [MaxLength(150, ErrorMessage = "La descripción no puede superar 150 caracteres.")]
    public string? Descripcion { get; set; }
}
