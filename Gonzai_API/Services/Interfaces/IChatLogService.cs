using Gonzai_API.DTOs.ChatLog;

namespace Gonzai_API.Services.Interfaces;

public interface IChatLogService
{
    Task<IEnumerable<ChatLogResponseDto>> GetAllAsync();
    Task<IEnumerable<ChatLogResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    Task<ChatLogResponseDto?> GetByIdAsync(int id);
    Task<IEnumerable<ChatLogResponseDto>> GetRecentByUsuarioIdAsync(int usuarioId, int limit = 10);
    Task<ChatLogResponseDto> CreateAsync(ChatLogCreateDto dto, string respuesta);
}
