using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.Comun;

public class AnulacionDto
{
    [Required(ErrorMessage = "El motivo de anulación es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El motivo no puede superar 150 caracteres.")]
    public string Motivo { get; set; } = string.Empty;
}
