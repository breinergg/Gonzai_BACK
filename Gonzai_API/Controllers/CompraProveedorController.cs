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

    // GET: api/compraproveedor?proveedorId=1&desde=2026-10-01&hasta=2026-10-31&page=1&pageSize=50
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? proveedorId,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (desde > hasta)
            return BadRequest(new { message = "La fecha 'desde' no puede ser mayor que 'hasta'." });

        if (page < 1) page = 1;
        if (pageSize is < 1 or > 200) pageSize = 50;

        return Ok(await _service.GetAllAsync(proveedorId, desde, hasta, page, pageSize));
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
        var compra = await _service.CreateAsync(dto, User.GetUsuarioId());
        return CreatedAtAction(nameof(GetById), new { id = compra.Id }, compra);
    }

    // POST: api/compraproveedor/5/anular
    [HttpPost("{id:int}/anular")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CompraProveedorResponseDto>> Anular(int id, [FromBody] AnulacionDto dto)
    {
        var compra = await _service.AnularAsync(id, dto.Motivo, User.GetUsuarioId());

        if (compra is null)
            return NotFound(new { message = $"Compra con Id {id} no encontrada." });

        return Ok(compra);
    }
}
