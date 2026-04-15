using Gonzai_API.DTOs.Cliente;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClienteController : ControllerBase
{
    private readonly IClienteService _service;

    public ClienteController(IClienteService service)
    {
        _service = service;
    }

    // GET: api/cliente
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClienteResponseDto>>> GetAll(
        [FromQuery] bool soloActivos = false)
    {
        var clientes = soloActivos
            ? await _service.GetAllActivosAsync()
            : await _service.GetAllAsync();

        return Ok(clientes);
    }

    // GET: api/cliente/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteResponseDto>> GetById(int id)
    {
        var cliente = await _service.GetByIdAsync(id);

        if (cliente is null)
            return NotFound(new { message = $"Cliente con Id {id} no encontrado." });

        return Ok(cliente);
    }

    // POST: api/cliente
    [HttpPost]
    public async Task<ActionResult<ClienteResponseDto>> Create([FromBody] ClienteCreateDto dto)
    {
        var cliente = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, cliente);
    }

    // PUT: api/cliente/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClienteResponseDto>> Update(int id, [FromBody] ClienteUpdateDto dto)
    {
        var cliente = await _service.UpdateAsync(id, dto);

        if (cliente is null)
            return NotFound(new { message = $"Cliente con Id {id} no encontrado." });

        return Ok(cliente);
    }

    // DELETE: api/cliente/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _service.DeleteAsync(id);

        if (!eliminado)
            return NotFound(new { message = $"Cliente con Id {id} no encontrado." });

        return NoContent();
    }
}
