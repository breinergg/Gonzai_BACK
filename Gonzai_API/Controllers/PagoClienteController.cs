using Gonzai_API.DTOs.Comprobante;
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
    private readonly IComprobanteService _comprobantes;
    private readonly IConfiguration _config;

    public PagoClienteController(IPagoClienteService service, IComprobanteService comprobantes, IConfiguration config)
    {
        _service = service;
        _comprobantes = comprobantes;
        _config = config;
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

    // GET: api/pagocliente/5/comprobante?formato=media-carta
    [HttpGet("{id:int}/comprobante")]
    public async Task<IActionResult> Comprobante(int id, [FromQuery] string? formato)
    {
        try
        {
            var (pdf, nombre) = await _comprobantes.AbonoPdfAsync(id, formato);
            return File(pdf, "application/pdf", nombre);
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

    [HttpGet("{id:int}/comprobante/compartir")]
    public async Task<ActionResult<CompartirComprobanteDto>> Compartir(
        int id, [FromQuery] string? formato, [FromQuery] bool regenerar = false)
    {
        try
        {
            return Ok(await _comprobantes.CompartirAbonoAsync(id, formato, regenerar, BasePublica()));
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

    [HttpPost("{id:int}/comprobante/enviar")]
    public async Task<IActionResult> Enviar(int id, [FromBody] EnviarComprobanteDto dto)
    {
        try
        {
            await _comprobantes.EnviarAbonoAsync(id, dto.Correo, dto.Formato, BasePublica());
            return Ok(new { message = $"Comprobante enviado a {dto.Correo}." });
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

    private string BasePublica()
    {
        var configurada = _config["Comprobantes:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configurada))
            return configurada.TrimEnd('/');

        return $"{Request.Scheme}://{Request.Host}";
    }
}
