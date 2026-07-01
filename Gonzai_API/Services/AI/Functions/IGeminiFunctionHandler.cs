using System.Text.Json;

namespace Gonzai_API.Services.AI.Functions;

public interface IGeminiFunctionHandler
{
    string FunctionName { get; }

    Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default);
}
