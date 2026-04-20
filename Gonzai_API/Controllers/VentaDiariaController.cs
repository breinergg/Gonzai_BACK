using Gonzai_API.DTOs.VentaDiaria;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VentaDiariaController : ControllerBase
{
    private readonly IVentaDiariaService _service;

    public VentaDiariaController(IVentaDiariaService service)
    {
        _service = service;
    }

    // GET: api/ventadiaria
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VentaDiariaResponseDto>>> GetAll()
    {
        var ventas = await _service.GetAllAsync();
        return Ok(ventas);
    }

    // GET: api/ventadiaria/resumen-mensual
    [HttpGet("resumen-mensual")]
    public async Task<ActionResult<VentaMensualResumenDto>> GetResumenMensual()
    {
        var resumen = await _service.GetResumenMensualAsync();
        return Ok(resumen);
    }

    // GET: api/ventadiaria/fecha/2026-04-14
    [HttpGet("fecha/{fecha}")]
    public async Task<ActionResult<IEnumerable<VentaDiariaResponseDto>>> GetByFecha(DateOnly fecha)
    {
        var ventas = await _service.GetByFechaAsync(fecha);
        return Ok(ventas);
    }

    // GET: api/ventadiaria/rango?desde=2026-04-01&hasta=2026-04-14
    [HttpGet("rango")]
    public async Task<ActionResult<IEnumerable<VentaDiariaResponseDto>>> GetByRango(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta)
    {
        if (desde > hasta)
            return BadRequest(new { message = "La fecha 'desde' no puede ser mayor que 'hasta'." });

        var ventas = await _service.GetByRangoFechaAsync(desde, hasta);
        return Ok(ventas);
    }

    // GET: api/ventadiaria/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<VentaDiariaResponseDto>> GetById(int id)
    {
        var venta = await _service.GetByIdAsync(id);

        if (venta is null)
            return NotFound(new { message = $"Venta diaria con Id {id} no encontrada." });

        return Ok(venta);
    }

    // POST: api/ventadiaria
    [HttpPost]
    public async Task<ActionResult<VentaDiariaResponseDto>> Create([FromBody] VentaDiariaCreateDto dto)
    {
        try
        {
            var venta = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = venta.Id }, venta);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // PUT: api/ventadiaria/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<VentaDiariaResponseDto>> Update(int id, [FromBody] VentaDiariaUpdateDto dto)
    {
        try
        {
            var venta = await _service.UpdateAsync(id, dto);

            if (venta is null)
                return NotFound(new { message = $"Venta diaria con Id {id} no encontrada." });

            return Ok(venta);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // DELETE: api/ventadiaria/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _service.DeleteAsync(id);

        if (!eliminado)
            return NotFound(new { message = $"Venta diaria con Id {id} no encontrada." });

        return NoContent();
    }
}
