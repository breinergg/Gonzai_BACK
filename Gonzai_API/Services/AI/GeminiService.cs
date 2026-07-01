using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gonzai_API.Services.Interfaces;

namespace Gonzai_API.Services.AI;

public class GeminiService : IAIService
{
    private const string NoReconocidaToken = "[NO_RECONOCIDA]";
    private const string ApiBaseUrl        = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
    private const int MaxIterations        = 5;
    private const int MaxHistorialMensajes = 4; // 4 intercambios = 8 mensajes — menor footprint de tokens

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GeminiService> _logger;
    private readonly IGeminiFunctionDispatcher _dispatcher;

    public GeminiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiService> logger,
        IGeminiFunctionDispatcher dispatcher)
    {
        _httpClient = httpClient;
        _logger     = logger;
        _dispatcher = dispatcher;
        _apiKey     = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("La clave 'Gemini:ApiKey' no está configurada en appsettings.");
    }

    public async Task<AiResult> GenerarRespuestaAsync(AiChatRequest request)
    {
        var contents      = BuildInitialContents(request);
        var executedCalls = new HashSet<string>(StringComparer.Ordinal);

        for (var iter = 0; iter < MaxIterations; iter++)
        {
            GeminiResponse? geminiResponse;

            try
            {
                geminiResponse = await CallGeminiWithRetryAsync(contents);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                _logger.LogError(ex, "Rate limit de Gemini agotado tras reintentos (iteración {Iter}).", iter + 1);
                return new AiResult(
                    "El servicio de IA está recibiendo muchas solicitudes en este momento. " +
                    "Por favor espera unos segundos e intenta de nuevo.", false);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error HTTP al comunicarse con Gemini (iteración {Iter}).", iter + 1);
                return new AiResult("No fue posible conectar con el servicio de IA. Intenta de nuevo.", true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en loop agéntico (iteración {Iter}).", iter + 1);
                return new AiResult("Ocurrió un error al procesar tu pregunta. Intenta de nuevo.", true);
            }

            var candidate = geminiResponse?.Candidates?.FirstOrDefault();
            if (candidate?.Content is null)
            {
                _logger.LogWarning("Gemini devolvió respuesta sin candidatos (iteración {Iter}).", iter + 1);
                break;
            }

            var parts         = candidate.Content.Parts;
            var functionCalls = parts
                .Where(p => p.FunctionCall is not null)
                .Select(p => p.FunctionCall!)
                .ToList();

            if (functionCalls.Count > 0)
            {
                _logger.LogInformation(
                    "Gemini solicitó {Count} function call(s) en iteración {Iter}: {Functions}",
                    functionCalls.Count,
                    iter + 1,
                    string.Join(", ", functionCalls.Select(c => c.Name)));

                contents.Add(candidate.Content);

                var responseParts = new List<GeminiPart>();

                foreach (var call in functionCalls)
                {
                    var callKey = $"{call.Name}:{call.Args.GetRawText()}";

                    string rawResult;
                    if (!executedCalls.Add(callKey))
                    {
                        _logger.LogWarning(
                            "Function call duplicada omitida: {Function} args={Args}",
                            call.Name, call.Args);

                        rawResult = """{"ok":false,"error":"LLAMADA_DUPLICADA","message":"Esta consulta ya fue ejecutada en este turno."}""";
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Ejecutando function call: {Function} args={Args}",
                            call.Name, call.Args);

                        try
                        {
                            rawResult = await _dispatcher.ExecuteAsync(call.Name, call.Args);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error ejecutando función {Function}.", call.Name);
                            rawResult = """{"ok":false,"error":"ERROR_INTERNO","message":"Error al ejecutar la función solicitada."}""";
                        }
                    }

                    var resultElement = JsonSerializer.Deserialize<JsonElement>(rawResult);

                    responseParts.Add(new GeminiPart(
                        FunctionResponse: new GeminiFunctionResponse(
                            Name:     call.Name,
                            Response: resultElement
                        )
                    ));
                }

                contents.Add(new GeminiContent(Role: "user", Parts: responseParts));
                continue;
            }

            var textPart = parts.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Text));
            if (textPart?.Text is { } text)
            {
                var esNoReconocida = text.Contains(NoReconocidaToken, StringComparison.OrdinalIgnoreCase);
                var respuesta      = esNoReconocida
                    ? "Lo siento, no puedo responder esa pregunta en este momento."
                    : text.Trim();

                return new AiResult(respuesta, esNoReconocida);
            }

            _logger.LogWarning("Gemini devolvió respuesta sin texto ni functionCall (iteración {Iter}).", iter + 1);
            break;
        }

        _logger.LogWarning("Se alcanzó el máximo de {Max} iteraciones.", MaxIterations);
        return new AiResult("No pude completar el procesamiento de tu pregunta. Intenta de nuevo.", true);
    }

    private async Task<GeminiResponse?> CallGeminiWithRetryAsync(
        List<GeminiContent> contents,
        int maxRetries = 3)
    {
        // Delays para el plan gratuito (15 RPM): ventana de 60s
        // intento 1 → falla → espera 20s
        // intento 2 → falla → espera 40s
        // intento 3 → falla → aborta
        var retryDelays = new[] { TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(40) };

        var url  = $"{ApiBaseUrl}?key={_apiKey}";
        var body = JsonSerializer.Serialize(BuildRequest(contents), JsonOptions);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            var httpContent = new StringContent(body, Encoding.UTF8, "application/json");
            var httpResp    = await _httpClient.PostAsync(url, httpContent);

            if (httpResp.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxRetries)
                {
                    _logger.LogError(
                        "Gemini devolvió 429 después de {MaxRetries} intentos. Abortando.",
                        maxRetries);
                    httpResp.EnsureSuccessStatusCode();
                }

                // Priorizar Retry-After del header si Gemini lo envía
                var retryAfter = httpResp.Headers.RetryAfter?.Delta
                    ?? retryDelays[Math.Min(attempt - 1, retryDelays.Length - 1)];

                _logger.LogWarning(
                    "Gemini 429 — límite de solicitudes alcanzado. Esperando {Segundos}s (intento {Actual}/{Max}).",
                    (int)retryAfter.TotalSeconds, attempt, maxRetries);

                await Task.Delay(retryAfter);
                continue;
            }

            httpResp.EnsureSuccessStatusCode();

            var json = await httpResp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GeminiResponse>(json, JsonOptions);
        }

        return null;
    }

    private static List<GeminiContent> BuildInitialContents(AiChatRequest request)
    {
        var contents = new List<GeminiContent>();

        if (request.Historial is { Count: > 0 })
        {
            var historial = request.Historial
                .Where(m => !string.IsNullOrWhiteSpace(m.Text))
                .TakeLast(MaxHistorialMensajes);

            foreach (var msg in historial)
            {
                var role = msg.Role.Equals("model", StringComparison.OrdinalIgnoreCase) ? "model" : "user";
                contents.Add(new GeminiContent(role, [new GeminiPart(Text: msg.Text)]));
            }
        }

        contents.Add(new GeminiContent("user", [new GeminiPart(Text: request.Pregunta)]));
        return contents;
    }

    private static GeminiRequest BuildRequest(List<GeminiContent> contents) => new(
        SystemInstruction: new GeminiContent(null, [new GeminiPart(Text: GeminiToolDefinitions.SystemPrompt)]),
        Contents:          contents,
        Tools:             GeminiToolDefinitions.BuildToolDeclarations(),
        GenerationConfig:  new GeminiGenerationConfig(Temperature: 0.2f, MaxOutputTokens: 1024)
    );
}

// =============================================================================
// Modelos internos para la API de Gemini
// =============================================================================

internal sealed record GeminiRequest(
    [property: JsonPropertyName("system_instruction")] GeminiContent           SystemInstruction,
    [property: JsonPropertyName("contents")]           List<GeminiContent>     Contents,
    [property: JsonPropertyName("tools")]              List<GeminiTool>?       Tools,
    [property: JsonPropertyName("generationConfig")]   GeminiGenerationConfig? GenerationConfig
);

internal sealed record GeminiContent(
    [property: JsonPropertyName("role")]  string?          Role,
    [property: JsonPropertyName("parts")] List<GeminiPart> Parts
);

internal sealed record GeminiPart(
    [property: JsonPropertyName("text")]             string?                 Text             = null,
    [property: JsonPropertyName("functionCall")]     GeminiFunctionCall?     FunctionCall     = null,
    [property: JsonPropertyName("functionResponse")] GeminiFunctionResponse? FunctionResponse = null
);

internal sealed record GeminiTool(
    [property: JsonPropertyName("functionDeclarations")] List<GeminiFunctionDeclaration> FunctionDeclarations
);

internal sealed record GeminiFunctionDeclaration(
    [property: JsonPropertyName("name")]        string                   Name,
    [property: JsonPropertyName("description")] string                   Description,
    [property: JsonPropertyName("parameters")]  GeminiFunctionParameters Parameters
);

internal sealed record GeminiFunctionParameters(
    [property: JsonPropertyName("type")]       string                                      Type,
    [property: JsonPropertyName("properties")] Dictionary<string, GeminiParameterProperty> Properties,
    [property: JsonPropertyName("required")]   List<string>                                Required
);

internal sealed record GeminiParameterProperty(
    [property: JsonPropertyName("type")]        string Type,
    [property: JsonPropertyName("description")] string Description
);

internal sealed record GeminiFunctionCall(
    [property: JsonPropertyName("name")] string      Name,
    [property: JsonPropertyName("args")] JsonElement Args
);

internal sealed record GeminiFunctionResponse(
    [property: JsonPropertyName("name")]     string      Name,
    [property: JsonPropertyName("response")] JsonElement Response
);

internal sealed record GeminiResponse(
    [property: JsonPropertyName("candidates")] List<GeminiCandidate>? Candidates
);

internal sealed record GeminiCandidate(
    [property: JsonPropertyName("content")] GeminiContent? Content
);

internal sealed record GeminiGenerationConfig(
    [property: JsonPropertyName("temperature")]     float Temperature,
    [property: JsonPropertyName("maxOutputTokens")] int   MaxOutputTokens
);
