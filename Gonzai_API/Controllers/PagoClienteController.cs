using Gonzai_API.DTOs.Comun;
using Gonzai_API.DTOs.PagoCliente;
using Gonzai_API.Extensions;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PagoClienteController : ControllerBase
{
    private readonly IPagoClienteService _service;

    public PagoClienteController(IPagoClienteService service)
    {
        _service = service;
    }

    // GET: api/pagocliente/venta/5
    [HttpGet("venta/{ventaClienteId:int}")]
    public async Task<ActionResult<IEnumerable<PagoClienteResponseDto>>> GetByVenta(int ventaClienteId)
    {
        return Ok(await _service.GetByVentaIdAsync(ventaClienteId));
    }

    // GET: api/pagocliente/cliente/3
    [HttpGet("cliente/{clienteId:int}")]
    public async Task<ActionResult<IEnumerable<PagoClienteResponseDto>>> GetByCliente(int clienteId)
    {
        return Ok(await _service.GetByClienteIdAsync(clienteId));
    }

    // GET: api/pagocliente/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PagoClienteResponseDto>> GetById(int id)
    {
        var pago = await _service.GetByIdAsync(id);

        if (pago is null)
            return NotFound(new { message = $"Pago con Id {id} no encontrado." });

        return Ok(pago);
    }

    // POST: api/pagocliente  (abono a una venta concreta)
    [HttpPost]
    public async Task<ActionResult<PagoClienteResponseDto>> Create([FromBody] PagoClienteCreateDto dto)
    {
        try
        {
            var pago = await _service.CreateAsync(dto, User.GetUsuarioId());
            return CreatedAtAction(nameof(GetById), new { id = pago.Id }, pago);
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

    // POST: api/pagocliente/5/anular
    [HttpPost("{id:int}/anular")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagoClienteResponseDto>> Anular(int id, [FromBody] AnulacionDto dto)
    {
        try
        {
            var pago = await _service.AnularAsync(id, dto.Motivo, User.GetUsuarioId());

            if (pago is null)
                return NotFound(new { message = $"Pago con Id {id} no encontrado." });

            return Ok(pago);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }
}
