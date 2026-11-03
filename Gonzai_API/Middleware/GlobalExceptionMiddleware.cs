using System.Net;
using System.Text.Json;

namespace Gonzai_API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
    {
        _next        = next;
        _logger      = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            KeyNotFoundException ex      => (HttpStatusCode.NotFound, ex.Message),
            InvalidOperationException ex => (HttpStatusCode.UnprocessableEntity, ex.Message),
            UnauthorizedAccessException  => (HttpStatusCode.Unauthorized, "No tienes permisos para realizar esta acción."),
            ArgumentException ex         => (HttpStatusCode.BadRequest, ex.Message),
            _ => (HttpStatusCode.InternalServerError, "Ocurrió un error interno. Intenta de nuevo más tarde.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Error no controlado en {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode  = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new { message };

        if (_environment.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError)
        {
            var devResponse = new
            {
                message,
                detail    = exception.Message,
                stackTrace = exception.StackTrace
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(devResponse));
            return;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
