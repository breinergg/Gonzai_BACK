namespace Gonzai_API.DTOs.ChatLog;

public class ChatLogResponseDto
{
    public int Id { get; set; }
    public int? UsuarioId { get; set; }
    public string Pregunta { get; set; } = string.Empty;
    public string? Respuesta { get; set; }
    public DateTime Fecha { get; set; }
}
