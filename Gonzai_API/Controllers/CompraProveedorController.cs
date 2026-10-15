using Gonzai_API.DTOs.Comun;
using Gonzai_API.DTOs.CompraProveedor;
using Gonzai_API.Extensions;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompraProveedorController : ControllerBase
{
    private readonly ICompraProveedorService _service;

    public CompraProveedorController(ICompraProveedorService service)
    {
        _service = service;
    }

    // GET: api/compraproveedor?proveedorId=1&desde=2026-10-01&hasta=2026-10-31
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompraProveedorResponseDto>>> GetAll(
        [FromQuery] int? proveedorId,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta)
    {
        if (desde > hasta)
            return BadRequest(new { message = "La fecha 'desde' no puede ser mayor que 'hasta'." });

        return Ok(await _service.GetAllAsync(proveedorId, desde, hasta));
    }

    // GET: api/compraproveedor/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompraProveedorResponseDto>> GetById(int id)
    {
        var compra = await _service.GetByIdAsync(id);

        if (compra is null)
            return NotFound(new { message = $"Compra con Id {id} no encontrada." });

        return Ok(compra);
    }

    // POST: api/compraproveedor
    [HttpPost]
    public async Task<ActionResult<CompraProveedorResponseDto>> Create([FromBody] CompraProveedorCreateDto dto)
    {
        try
        {
            var compra = await _service.CreateAsync(dto, User.GetUsuarioId());
            return CreatedAtAction(nameof(GetById), new { id = compra.Id }, compra);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    // POST: api/compraproveedor/5/anular
    [HttpPost("{id:int}/anular")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CompraProveedorResponseDto>> Anular(int id, [FromBody] AnulacionDto dto)
    {
        try
        {
            var compra = await _service.AnularAsync(id, dto.Motivo, User.GetUsuarioId());

            if (compra is null)
                return NotFound(new { message = $"Compra con Id {id} no encontrada." });

            return Ok(compra);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }
}
