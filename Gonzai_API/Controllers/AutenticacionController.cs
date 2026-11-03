using Gonzai_API.DTOs.Usuario;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gonzai_API.Controllers;

/// <summary>
/// Login. Va aparte de <c>UsuarioController</c> porque es la única operación que ocurre
/// sin empresa conocida: aquí todavía no se sabe a qué base de datos hay que ir.
/// La ruta pública no cambia: sigue siendo POST /api/usuario/login.
/// </summary>
[ApiController]
[Route("api/usuario")]
public class AutenticacionController : ControllerBase
{
    private readonly IAutenticacionService _service;

    public AutenticacionController(IAutenticacionService service)
    {
        _service = service;
    }

    // POST: api/usuario/login
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var token = await _service.LoginAsync(dto, ct);

        if (token is null)
            return Unauthorized(new { message = "Credenciales inválidas." });

        return Ok(token);
    }
}
