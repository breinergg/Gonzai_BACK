using Gonzai_API.DTOs.MovimientoInventario;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MovimientoInventarioController : ControllerBase
{
    private readonly IMovimientoInventarioService _service;

    public MovimientoInventarioController(IMovimientoInventarioService service)
    {
        _service = service;
    }

    // GET: api/movimientoinventario
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovimientoInventarioResponseDto>>> GetAll()
    {
        var movimientos = await _service.GetAllAsync();
        return Ok(movimientos);
    }

    // GET: api/movimientoinventario/producto/5
    [HttpGet("producto/{productoId:int}")]
    public async Task<ActionResult<IEnumerable<MovimientoInventarioResponseDto>>> GetByProducto(int productoId)
    {
        var movimientos = await _service.GetByProductoIdAsync(productoId);
        return Ok(movimientos);
    }

    // GET: api/movimientoinventario/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovimientoInventarioResponseDto>> GetById(int id)
    {
        var movimiento = await _service.GetByIdAsync(id);

        if (movimiento is null)
            return NotFound(new { message = $"Movimiento con Id {id} no encontrado." });

        return Ok(movimiento);
    }

    // POST: api/movimientoinventario
    [HttpPost]
    public async Task<ActionResult<MovimientoInventarioResponseDto>> Create([FromBody] MovimientoInventarioCreateDto dto)
    {
        try
        {
            var movimiento = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = movimiento.Id }, movimiento);
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
}
