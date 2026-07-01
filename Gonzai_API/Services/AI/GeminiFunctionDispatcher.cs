using System.Text.Json;
using Gonzai_API.Services.AI.Functions;
using Gonzai_API.Services.AI.Models;
using Gonzai_API.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Gonzai_API.Services.AI;

public class GeminiFunctionDispatcher : IGeminiFunctionDispatcher
{
    private readonly IReadOnlyDictionary<string, IGeminiFunctionHandler> _handlers;
    private readonly ILogger<GeminiFunctionDispatcher> _logger;

    public GeminiFunctionDispatcher(
        IEnumerable<IGeminiFunctionHandler> handlers,
        ILogger<GeminiFunctionDispatcher> logger)
    {
        _handlers = handlers.ToDictionary(h => h.FunctionName, StringComparer.OrdinalIgnoreCase);
        _logger   = logger;
    }

    public async Task<string> ExecuteAsync(string functionName, JsonElement args)
    {
        if (!_handlers.TryGetValue(functionName, out var handler))
        {
            _logger.LogWarning("Función no reconocida: {FunctionName}", functionName);
            return FunctionResult
                .Failure("FUNCION_NO_RECONOCIDA", $"La función '{functionName}' no está disponible.")
                .ToJson();
        }

        return await handler.ExecuteAsync(args);
    }
}
