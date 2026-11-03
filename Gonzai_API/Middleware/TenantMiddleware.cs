using Gonzai_API.Services.Tenancy;

namespace Gonzai_API.Middleware;

/// <summary>
/// Lee del token JWT a qué empresa pertenece el usuario y deja esa empresa fijada para toda
/// la petición. A partir de aquí, <c>AppDbContext</c> se conecta a la base de esa empresa y
/// de ninguna otra.
/// </summary>
public class TenantMiddleware
{
    public const string ClaimEmpresa = "empresa";

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next    = next;
        _logger  = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenant, ITenantStore store)
    {
        var claim = context.User.FindFirst(ClaimEmpresa)?.Value;

        if (int.TryParse(claim, out var empresaId) && tenant is TenantContext concreto)
        {
            var empresa = await store.PorIdAsync(empresaId, context.RequestAborted);

            if (empresa is null || !empresa.Activa)
            {
                _logger.LogWarning("Token con empresa {EmpresaId} inexistente o inactiva.", empresaId);
                context.Response.StatusCode  = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Tu empresa no está activa. Comunícate con el administrador."
                });
                return;
            }

            concreto.Establecer(empresa, store.CadenaConexion(empresa));
        }

        await _next(context);
    }
}
