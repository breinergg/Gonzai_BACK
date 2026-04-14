using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.VentaDiaria;

public class VentaDiariaUpdateDto
{
    [Required(ErrorMessage = "La fecha es obligatoria.")]
    public DateOnly Fecha { get; set; }

    [Required(ErrorMessage = "El total es obligatorio.")]
    [Range(0, double.MaxValue, ErrorMessage = "El total debe ser mayor o igual a 0.")]
    public decimal Total { get; set; }

    [MaxLength(150, ErrorMessage = "La descripción no puede superar 150 caracteres.")]
    public string? Descripcion { get; set; }

    public int? UsuarioId { get; set; }
}
