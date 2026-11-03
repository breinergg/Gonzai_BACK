using Gonzai_API.DTOs.Comprobante;
using Gonzai_API.DTOs.Comun;
using Gonzai_API.DTOs.VentaCliente;
using Gonzai_API.Extensions;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VentaClienteController : ControllerBase
{
    private readonly IVentaClienteService _service;
    private readonly IComprobanteService _comprobantes;
    private readonly IConfiguration _config;

    public VentaClienteController(IVentaClienteService service, IComprobanteService comprobantes, IConfiguration config)
    {
        _service = service;
        _comprobantes = comprobantes;
        _config = config;
    }

    // GET: api/ventacliente?clienteId=3&desde=2026-10-01&hasta=2026-10-31&soloPendientes=true&page=1&pageSize=50
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? clienteId,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] bool soloPendientes = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (desde > hasta)
            return BadRequest(new { message = "La fecha 'desde' no puede ser mayor que 'hasta'." });

        if (page < 1) page = 1;
        if (pageSize is < 1 or > 200) pageSize = 50;

        return Ok(await _service.GetAllAsync(clienteId, desde, hasta, soloPendientes, page, pageSize));
    }

    // GET: api/ventacliente/resumen-mensual
    [HttpGet("resumen-mensual")]
    public async Task<ActionResult<VentaMensualResumenDto>> GetResumenMensual()
    {
        return Ok(await _service.GetResumenMensualAsync());
    }

    // GET: api/ventacliente/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<VentaClienteResponseDto>> GetById(int id)
    {
        var venta = await _service.GetByIdAsync(id);

        if (venta is null)
            return NotFound(new { message = $"Venta con Id {id} no encontrada." });

        return Ok(venta);
    }

    // POST: api/ventacliente
    [HttpPost]
    public async Task<ActionResult<VentaClienteResponseDto>> Create([FromBody] VentaClienteCreateDto dto)
    {
        var venta = await _service.CreateAsync(dto, User.GetUsuarioId());
        return CreatedAtAction(nameof(GetById), new { id = venta.Id }, venta);
    }

    // POST: api/ventacliente/5/anular
    [HttpPost("{id:int}/anular")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<VentaClienteResponseDto>> Anular(int id, [FromBody] AnulacionDto dto)
    {
        var venta = await _service.AnularAsync(id, dto.Motivo, User.GetUsuarioId());

        if (venta is null)
            return NotFound(new { message = $"Venta con Id {id} no encontrada." });

        return Ok(venta);
    }

    // GET: api/ventacliente/11/comprobante?formato=termico
    [HttpGet("{id:int}/comprobante")]
    public async Task<IActionResult> Comprobante(int id, [FromQuery] string? formato)
    {
        var (pdf, nombre) = await _comprobantes.VentaPdfAsync(id, formato);
        return File(pdf, "application/pdf", nombre);
    }

    // GET: api/ventacliente/11/comprobante/compartir?formato=termico&regenerar=false
    [HttpGet("{id:int}/comprobante/compartir")]
    public async Task<ActionResult<CompartirComprobanteDto>> Compartir(
        int id, [FromQuery] string? formato, [FromQuery] bool regenerar = false)
    {
        return Ok(await _comprobantes.CompartirVentaAsync(id, formato, regenerar, BasePublica()));
    }

    // POST: api/ventacliente/11/comprobante/enviar
    [HttpPost("{id:int}/comprobante/enviar")]
    public async Task<IActionResult> Enviar(int id, [FromBody] EnviarComprobanteDto dto)
    {
        await _comprobantes.EnviarVentaAsync(id, dto.Correo, dto.Formato, BasePublica());
        return Ok(new { message = $"Comprobante enviado a {dto.Correo}." });
    }

    private string BasePublica()
    {
        var configurada = _config["Comprobantes:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configurada))
            return configurada.TrimEnd('/');

        return $"{Request.Scheme}://{Request.Host}";
    }
}
