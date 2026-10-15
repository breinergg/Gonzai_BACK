using Gonzai_API.DTOs.Producto;
using Gonzai_API.Extensions;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductoController : ControllerBase
{
    private readonly IProductoService _service;

    public ProductoController(IProductoService service)
    {
        _service = service;
    }

    // GET: api/producto
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductoResponseDto>>> GetAll(
        [FromQuery] bool soloActivos = false)
    {
        var productos = soloActivos
            ? await _service.GetAllActivosAsync()
            : await _service.GetAllAsync();

        return Ok(productos);
    }

    // GET: api/producto/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductoResponseDto>> GetById(int id)
    {
        var producto = await _service.GetByIdAsync(id);

        if (producto is null)
            return NotFound(new { message = $"Producto con Id {id} no encontrado." });

        return Ok(producto);
    }

    // POST: api/producto
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductoResponseDto>> Create([FromBody] ProductoCreateDto dto)
    {
        try
        {
            var producto = await _service.CreateAsync(dto, User.GetUsuarioId());
            return CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto);
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

    // PUT: api/producto/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductoResponseDto>> Update(int id, [FromBody] ProductoUpdateDto dto)
    {
        try
        {
            var producto = await _service.UpdateAsync(id, dto);

            if (producto is null)
                return NotFound(new { message = $"Producto con Id {id} no encontrado." });

            return Ok(producto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // DELETE: api/producto/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _service.DeleteAsync(id);

        if (!eliminado)
            return NotFound(new { message = $"Producto con Id {id} no encontrado." });

        return NoContent();
    }

    // GET: api/producto/count
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetProductosActivosCount()
    {
        var count = await _service.GetProductosActivosCountAsync();
        return Ok(new { productosActivos = count });
    }

    // GET: api/producto/variante/5
    [HttpGet("variante/{varianteId:int}")]
    public async Task<ActionResult<ProductoVarianteResponseDto>> GetVariante(int varianteId)
    {
        var variante = await _service.GetVarianteByIdAsync(varianteId);

        if (variante is null)
            return NotFound(new { message = $"Variante con Id {varianteId} no encontrada." });

        return Ok(variante);
    }

    // POST: api/producto/5/variantes
    [HttpPost("{id:int}/variantes")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductoVarianteResponseDto>> CreateVariante(int id, [FromBody] ProductoVarianteCreateDto dto)
    {
        try
        {
            var variante = await _service.CreateVarianteAsync(id, dto, User.GetUsuarioId());
            return CreatedAtAction(nameof(GetVariante), new { varianteId = variante.Id }, variante);
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

    // POST: api/producto/5/variantes/matriz  { "tallas": ["S","M","L"], "colores": ["Negro","Blanco"] }
    [HttpPost("{id:int}/variantes/matriz")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<ProductoVarianteResponseDto>>> CreateMatriz(
        int id, [FromBody] ProductoVarianteMatrizDto dto)
    {
        try
        {
            var variantes = await _service.CreateMatrizAsync(id, dto, User.GetUsuarioId());
            return StatusCode(StatusCodes.Status201Created, variantes);
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

    // PUT: api/producto/variante/5
    [HttpPut("variante/{varianteId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductoVarianteResponseDto>> UpdateVariante(
        int varianteId, [FromBody] ProductoVarianteUpdateDto dto)
    {
        try
        {
            var variante = await _service.UpdateVarianteAsync(varianteId, dto);

            if (variante is null)
                return NotFound(new { message = $"Variante con Id {varianteId} no encontrada." });

            return Ok(variante);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    // DELETE: api/producto/variante/5 (desactiva; conserva su historial)
    [HttpDelete("variante/{varianteId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteVariante(int varianteId)
    {
        if (!await _service.DeleteVarianteAsync(varianteId))
            return NotFound(new { message = $"Variante con Id {varianteId} no encontrada." });

        return NoContent();
    }
}
