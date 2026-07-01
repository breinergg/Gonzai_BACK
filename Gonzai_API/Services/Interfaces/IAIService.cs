namespace Gonzai_API.Services.Interfaces;

public record AiChatMessage(string Role, string Text);

public record AiChatRequest(string Pregunta, IReadOnlyList<AiChatMessage>? Historial = null);

public record AiResult(string Respuesta, bool EsNoReconocida);

public interface IAIService
{
    Task<AiResult> GenerarRespuestaAsync(AiChatRequest request);
}
