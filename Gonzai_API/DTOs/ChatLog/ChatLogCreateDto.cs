using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.ChatLog;

public class ChatLogCreateDto
{
    public int? UsuarioId { get; set; }

    [Required(ErrorMessage = "La pregunta es obligatoria.")]
    public string Pregunta { get; set; } = string.Empty;
}
