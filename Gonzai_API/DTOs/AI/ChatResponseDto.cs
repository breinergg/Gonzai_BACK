namespace Gonzai_API.DTOs.AI;

public class ChatResponseDto
{
    public int ChatLogId { get; set; }
    public string Pregunta { get; set; } = string.Empty;
    public string Respuesta { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
