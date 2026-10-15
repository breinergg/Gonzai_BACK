using Gonzai_API.DTOs.Proveedor;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProveedorController : ControllerBase
{
    private readonly IProveedorService _service;

    public ProveedorController(IProveedorService service)
    {
        _service = service;
    }

    // GET: api/proveedor?soloActivos=true
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProveedorResponseDto>>> GetAll([FromQuery] bool soloActivos = false)
    {
        return Ok(await _service.GetAllAsync(soloActivos));
    }

    // GET: api/proveedor/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProveedorResponseDto>> GetById(int id)
    {
        var proveedor = await _service.GetByIdAsync(id);

        if (proveedor is null)
            return NotFound(new { message = $"Proveedor con Id {id} no encontrado." });

        return Ok(proveedor);
    }

    // POST: api/proveedor
    [HttpPost]
    public async Task<ActionResult<ProveedorResponseDto>> Create([FromBody] ProveedorCreateDto dto)
    {
        var proveedor = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = proveedor.Id }, proveedor);
    }

    // PUT: api/proveedor/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProveedorResponseDto>> Update(int id, [FromBody] ProveedorUpdateDto dto)
    {
        var proveedor = await _service.UpdateAsync(id, dto);

        if (proveedor is null)
            return NotFound(new { message = $"Proveedor con Id {id} no encontrado." });

        return Ok(proveedor);
    }

    // DELETE: api/proveedor/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _service.DeleteAsync(id);

        if (!eliminado)
            return NotFound(new { message = $"Proveedor con Id {id} no encontrado." });

        return NoContent();
    }
}
