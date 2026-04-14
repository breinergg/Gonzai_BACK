using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.PreguntaNoReconocida;

public class PreguntaNoReconocidaCreateDto
{
    [Required(ErrorMessage = "La pregunta es obligatoria.")]
    public string Pregunta { get; set; } = string.Empty;
}
