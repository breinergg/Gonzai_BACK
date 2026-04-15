using Gonzai_API.DTOs.Usuario;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _service;

    public UsuarioController(IUsuarioService service)
    {
        _service = service;
    }

    // POST: api/usuario/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto dto)
    {
        var token = await _service.LoginAsync(dto);

        if (token is null)
            return Unauthorized(new { message = "Credenciales inválidas." });

        return Ok(token);
    }

    // GET: api/usuario
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetAll()
    {
        var usuarios = await _service.GetAllAsync();
        return Ok(usuarios);
    }

    // GET: api/usuario/5
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UsuarioResponseDto>> GetById(int id)
    {
        var usuario = await _service.GetByIdAsync(id);

        if (usuario is null)
            return NotFound(new { message = $"Usuario con Id {id} no encontrado." });

        return Ok(usuario);
    }

    // POST: api/usuario
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UsuarioResponseDto>> Create([FromBody] UsuarioCreateDto dto)
    {
        try
        {
            var usuario = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, usuario);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT: api/usuario/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UsuarioResponseDto>> Update(int id, [FromBody] UsuarioUpdateDto dto)
    {
        try
        {
            var usuario = await _service.UpdateAsync(id, dto);

            if (usuario is null)
                return NotFound(new { message = $"Usuario con Id {id} no encontrado." });

            return Ok(usuario);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE: api/usuario/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _service.DeleteAsync(id);

        if (!eliminado)
            return NotFound(new { message = $"Usuario con Id {id} no encontrado." });

        return NoContent();
    }
}
