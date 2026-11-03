using Gonzai_API.DTOs.AI;
using Gonzai_API.DTOs.ChatLog;
using Gonzai_API.DTOs.PreguntaNoReconocida;
using Gonzai_API.Extensions;
using Gonzai_API.Services.Interfaces;
using Gonzai_API.Services.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly IChatLogService _chatLogService;
    private readonly IPreguntaNoReconocidaService _preguntaNoReconocidaService;
    private readonly ITenantContext _tenant;
    private readonly ITenantStore _empresas;

    public AiController(
        IAIService aiService,
        IChatLogService chatLogService,
        IPreguntaNoReconocidaService preguntaNoReconocidaService,
        ITenantContext tenant,
        ITenantStore empresas)
    {
        _aiService = aiService;
        _chatLogService = chatLogService;
        _preguntaNoReconocidaService = preguntaNoReconocidaService;
        _tenant = tenant;
        _empresas = empresas;
    }

    // POST: api/ai/chat
    [HttpPost("chat")]
    [EnableRateLimiting("ai-chat")]
    public async Task<ActionResult<ChatResponseDto>> Chat([FromBody] ChatRequestDto dto)
    {
        // Cada consulta a la IA se cobra a la cuenta de Gemini de la plataforma, así que
        // cada empresa tiene su cupo diario.
        var empresa = await _empresas.PorIdAsync(_tenant.EmpresaIdRequerido);

        if (empresa is not null && await _chatLogService.ContarDelDiaAsync() >= empresa.LimiteChatsDiarios)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                message = empresa.LimiteChatsDiarios == 0
                    ? "El asistente de IA no está habilitado para tu empresa."
                    : $"Tu empresa alcanzó el límite de {empresa.LimiteChatsDiarios} consultas a la IA por día. " +
                      "Vuelve a intentarlo mañana."
            });
        }

        var usuarioId = User.GetUsuarioId();
        var historial = await BuildHistorialAsync(dto, usuarioId);

        var resultado = await _aiService.GenerarRespuestaAsync(
            new AiChatRequest(dto.Pregunta, historial));

        var log = await _chatLogService.CreateAsync(
            new ChatLogCreateDto { UsuarioId = usuarioId, Pregunta = dto.Pregunta },
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

    private async Task<IReadOnlyList<AiChatMessage>?> BuildHistorialAsync(ChatRequestDto dto, int usuarioId)
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

        if (dto.UsarHistorialDeBd)
        {
            var logs = await _chatLogService.GetRecentByUsuarioIdAsync(usuarioId, 10);

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
