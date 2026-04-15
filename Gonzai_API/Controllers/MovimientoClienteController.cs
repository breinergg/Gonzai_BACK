using Gonzai_API.DTOs.MovimientoCliente;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MovimientoClienteController : ControllerBase
{
    private readonly IMovimientoClienteService _service;

    public MovimientoClienteController(IMovimientoClienteService service)
    {
        _service = service;
    }

    // GET: api/movimientocliente
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovimientoClienteResponseDto>>> GetAll()
    {
        var movimientos = await _service.GetAllAsync();
        return Ok(movimientos);
    }

    // GET: api/movimientocliente/cliente/5
    [HttpGet("cliente/{clienteId:int}")]
    public async Task<ActionResult<IEnumerable<MovimientoClienteResponseDto>>> GetByCliente(int clienteId)
    {
        var movimientos = await _service.GetByClienteIdAsync(clienteId);
        return Ok(movimientos);
    }

    // GET: api/movimientocliente/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovimientoClienteResponseDto>> GetById(int id)
    {
        var movimiento = await _service.GetByIdAsync(id);

        if (movimiento is null)
            return NotFound(new { message = $"Movimiento con Id {id} no encontrado." });

        return Ok(movimiento);
    }

    // POST: api/movimientocliente
    [HttpPost]
    public async Task<ActionResult<MovimientoClienteResponseDto>> Create([FromBody] MovimientoClienteCreateDto dto)
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
    }
}
