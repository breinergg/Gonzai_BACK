using System.Text.Json;

namespace Gonzai_API.Services.Interfaces;

public interface IGeminiFunctionDispatcher
{
    /// <summary>
    /// Ejecuta la función solicitada por Gemini y devuelve el resultado como JSON compacto.
    /// </summary>
    Task<string> ExecuteAsync(string functionName, JsonElement args);
}
