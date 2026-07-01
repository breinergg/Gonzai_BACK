using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.AI;

public class ChatMessageDto
{
    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string Text { get; set; } = string.Empty;
}

public class ChatRequestDto
{
    public int? UsuarioId { get; set; }

    [Required(ErrorMessage = "La pregunta es obligatoria.")]
    [MaxLength(2000, ErrorMessage = "La pregunta no puede superar los 2000 caracteres.")]
    public string Pregunta { get; set; } = string.Empty;

    /// <summary>
    /// Historial opcional de la conversación. Si no se envía y hay UsuarioId,
    /// se cargará automáticamente desde ChatLogs.
    /// </summary>
    public List<ChatMessageDto>? Historial { get; set; }

    /// <summary>
    /// Si es true y no se envía Historial, carga los últimos mensajes del usuario desde BD.
    /// </summary>
    public bool UsarHistorialDeBd { get; set; } = true;
}
