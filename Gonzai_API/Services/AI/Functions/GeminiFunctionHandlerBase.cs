using Gonzai_API.Services.AI.Models;

namespace Gonzai_API.Services.AI.Functions;

public abstract class GeminiFunctionHandlerBase : IGeminiFunctionHandler
{
    public abstract string FunctionName { get; }

    public abstract Task<string> ExecuteAsync(
        System.Text.Json.JsonElement args,
        CancellationToken cancellationToken = default);

    protected static string Success(object data, object? meta = null) =>
        FunctionResult.Success(data, meta).ToJson();

    protected static string Failure(string error, string message, object? data = null) =>
        FunctionResult.Failure(error, message, data).ToJson();
}
