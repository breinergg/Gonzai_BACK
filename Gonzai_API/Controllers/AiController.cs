using Gonzai_API.DTOs.AI;
using Gonzai_API.DTOs.ChatLog;
using Gonzai_API.DTOs.PreguntaNoReconocida;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly IChatLogService _chatLogService;
    private readonly IPreguntaNoReconocidaService _preguntaNoReconocidaService;

    public AiController(
        IAIService aiService,
        IChatLogService chatLogService,
        IPreguntaNoReconocidaService preguntaNoReconocidaService)
    {
        _aiService = aiService;
        _chatLogService = chatLogService;
        _preguntaNoReconocidaService = preguntaNoReconocidaService;
    }

    // POST: api/ai/chat
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponseDto>> Chat([FromBody] ChatRequestDto dto)
    {
        var historial = await BuildHistorialAsync(dto);

        var resultado = await _aiService.GenerarRespuestaAsync(
            new AiChatRequest(dto.Pregunta, historial));

        var log = await _chatLogService.CreateAsync(
            new ChatLogCreateDto { UsuarioId = dto.UsuarioId, Pregunta = dto.Pregunta },
            resultado.Respuesta);

        if (resultado.EsNoReconocida)
            await _preguntaNoReconocidaService.CreateAsync(
                new PreguntaNoReconocidaCreateDto { Pregunta = dto.Pregunta });

        return Ok(new ChatResponseDto
        {
            ChatLogId = log.Id,
            Pregunta  = log.Pregunta,
            Respuesta = log.Respuesta ?? string.Empty,
            Fecha     = log.Fecha
        });
    }

    private async Task<IReadOnlyList<AiChatMessage>?> BuildHistorialAsync(ChatRequestDto dto)
    {
        if (dto.Historial is { Count: > 0 })
        {
            return dto.Historial
                .Where(m => !string.IsNullOrWhiteSpace(m.Text))
                .Select(m => new AiChatMessage(
                    m.Role.Equals("model", StringComparison.OrdinalIgnoreCase) ? "model" : "user",
                    m.Text))
                .ToList();
        }

        if (dto.UsarHistorialDeBd && dto.UsuarioId.HasValue)
        {
            var logs = await _chatLogService.GetRecentByUsuarioIdAsync(dto.UsuarioId.Value, 10);

            return logs
                .SelectMany(l => new[]
                {
                    new AiChatMessage("user",  l.Pregunta),
                    new AiChatMessage("model", l.Respuesta ?? string.Empty)
                })
                .Where(m => !string.IsNullOrWhiteSpace(m.Text))
                .ToList();
        }

        return null;
    }
}
