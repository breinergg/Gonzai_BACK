using System.Security.Claims;

namespace Gonzai_API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Id del usuario autenticado, tomado del JWT. Nunca se acepta desde el body
    /// para que nadie pueda registrar operaciones a nombre de otro usuario.
    /// </summary>
    public static int GetUsuarioId(this ClaimsPrincipal user)
    {
        var valor = user.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(valor, out var id)
            ? id
            : throw new UnauthorizedAccessException("El token no contiene un usuario válido.");
    }
}
