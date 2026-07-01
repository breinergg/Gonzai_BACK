using System.Text.Json;

namespace Gonzai_API.Services.AI.Helpers;

internal static class FunctionArgsHelper
{
    public static string? GetString(JsonElement args, string name)
    {
        if (!args.TryGetProperty(name, out var prop))
            return null;

        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;
    }

    public static int? GetInt(JsonElement args, string name)
    {
        if (!args.TryGetProperty(name, out var prop))
            return null;

        return prop.TryGetInt32(out var value) ? value : null;
    }

    public static bool GetBool(JsonElement args, string name, bool defaultValue = false)
    {
        if (!args.TryGetProperty(name, out var prop))
            return defaultValue;

        return prop.ValueKind switch
        {
            JsonValueKind.True  => true,
            JsonValueKind.False => false,
            _                   => defaultValue
        };
    }
}
