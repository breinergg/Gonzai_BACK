using Gonzai_API.DTOs.Lote;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

/// <summary>
/// Vista de negocio de los lotes. Un lote es una compra a proveedor: se crea y se anula en
/// api/compraproveedor; aquí se consulta su avance y rentabilidad.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoteController : ControllerBase
{
    private readonly ILoteService _service;

    public LoteController(ILoteService service)
    {
        _service = service;
    }

    // GET: api/lote?proveedorId=1&soloConStock=true
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoteResumenDto>>> GetAll(
        [FromQuery] int? proveedorId, [FromQuery] bool soloConStock = false)
    {
        return Ok(await _service.GetAllAsync(proveedorId, soloConStock));
    }

    // GET: api/lote/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LoteDetalleDto>> GetById(int id)
    {
        var lote = await _service.GetByIdAsync(id);

        if (lote is null)
            return NotFound(new { message = $"Lote con Id {id} no encontrado." });

        return Ok(lote);
    }

    // GET: api/lote/codigo/L-2026-0015
    [HttpGet("codigo/{codigo}")]
    public async Task<ActionResult<LoteDetalleDto>> GetByCodigo(string codigo)
    {
        var lote = await _service.GetByCodigoAsync(codigo);

        if (lote is null)
            return NotFound(new { message = $"Lote {codigo} no encontrado." });

        return Ok(lote);
    }

    // GET: api/lote/disponibles?productoVarianteId=12  (o ?productoId=3 para todas sus tallas/colores)
    // Orden FIFO: la primera fila es la que se vende si la venta no elige lote.
    [HttpGet("disponibles")]
    public async Task<ActionResult<IEnumerable<ExistenciaDisponibleDto>>> GetDisponibles(
        [FromQuery] int? productoId, [FromQuery] int? productoVarianteId)
    {
        if (productoId is null && productoVarianteId is null)
            return BadRequest(new { message = "Indique productoId o productoVarianteId." });

        return Ok(await _service.GetDisponiblesAsync(productoId, productoVarianteId));
    }
}
