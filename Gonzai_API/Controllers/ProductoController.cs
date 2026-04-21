using Gonzai_API.DTOs.Producto;
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
            var producto = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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
}
