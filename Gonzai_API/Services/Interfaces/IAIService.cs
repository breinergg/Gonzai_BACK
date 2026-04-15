namespace Gonzai_API.Services.Interfaces;

public interface IAIService
{
    Task<string> GenerarRespuestaAsync(string pregunta);
}
