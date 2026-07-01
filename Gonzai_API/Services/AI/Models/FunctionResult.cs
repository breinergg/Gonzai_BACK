using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gonzai_API.Services.AI.Models;

public sealed class FunctionResult
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public bool Ok { get; init; }
    public object? Data { get; init; }
    public string? Error { get; init; }
    public string? Message { get; init; }
    public object? Meta { get; init; }

    public static FunctionResult Success(object data, object? meta = null) => new()
    {
        Ok   = true,
        Data = data,
        Meta = meta
    };

    public static FunctionResult Failure(string error, string message, object? data = null) => new()
    {
        Ok      = false,
        Error   = error,
        Message = message,
        Data    = data
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
}
