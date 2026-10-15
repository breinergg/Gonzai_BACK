using Gonzai_API.DTOs.MovimientoInventario;
using Gonzai_API.Extensions;
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

    // POST: api/movimientoinventario/ajuste
    // Las entradas por compra y salidas por venta se generan solas; esto es solo para ajustes.
    [HttpPost("ajuste")]
    public async Task<ActionResult<IEnumerable<MovimientoInventarioResponseDto>>> CreateAjuste(
        [FromBody] MovimientoInventarioCreateDto dto)
    {
        try
        {
            var movimientos = await _service.CreateAjusteAsync(dto, User.GetUsuarioId());
            return StatusCode(StatusCodes.Status201Created, movimientos);
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
