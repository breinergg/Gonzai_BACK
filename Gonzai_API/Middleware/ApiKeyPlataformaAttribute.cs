using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Gonzai_API.Middleware;

/// <summary>
/// Protege las operaciones de la plataforma (dar de alta empresas). Quien las usa es el dueño
/// de GonzAI, no el usuario de una empresa, así que no sirve el token normal: va con una clave
/// propia en la cabecera. Si la clave no está configurada, estas rutas quedan cerradas.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ApiKeyPlataformaAttribute : Attribute, IAsyncActionFilter
{
    public const string Header = "X-Plataforma-ApiKey";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var config    = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var esperada  = config["Plataforma:ApiKey"];

        if (string.IsNullOrWhiteSpace(esperada))
        {
            context.Result = new ObjectResult(new
            {
                message = "La administración de la plataforma está deshabilitada. Configura Plataforma:ApiKey."
            })
            { StatusCode = StatusCodes.Status503ServiceUnavailable };
            return;
        }

        var recibida = context.HttpContext.Request.Headers[Header].ToString();

        if (!SonIguales(recibida, esperada))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Clave de plataforma inválida." });
            return;
        }

        await next();
    }

    /// <summary>Comparación en tiempo constante: no revela la clave por cuánto tarda en fallar.</summary>
    private static bool SonIguales(string recibida, string esperada) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(recibida.PadRight(esperada.Length)[..esperada.Length]),
            Encoding.UTF8.GetBytes(esperada))
        && recibida.Length == esperada.Length;
}
