using Gonzai_API.DTOs.Categoria;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriaController : ControllerBase
{
    private readonly ICategoriaService _service;

    public CategoriaController(ICategoriaService service)
    {
        _service = service;
    }

    // GET: api/categoria
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoriaResponseDto>>> GetAll()
    {
        var categorias = await _service.GetAllAsync();
        return Ok(categorias);
    }

    // GET: api/categoria/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoriaResponseDto>> GetById(int id)
    {
        var categoria = await _service.GetByIdAsync(id);

        if (categoria is null)
            return NotFound(new { message = $"Categoría con Id {id} no encontrada." });

        return Ok(categoria);
    }

    // POST: api/categoria
    [HttpPost]
    public async Task<ActionResult<CategoriaResponseDto>> Create([FromBody] CategoriaCreateDto dto)
    {
        var categoria = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria);
    }

    // PUT: api/categoria/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoriaResponseDto>> Update(int id, [FromBody] CategoriaUpdateDto dto)
    {
        var categoria = await _service.UpdateAsync(id, dto);

        if (categoria is null)
            return NotFound(new { message = $"Categoría con Id {id} no encontrada." });

        return Ok(categoria);
    }

    // DELETE: api/categoria/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _service.DeleteAsync(id);

        if (!eliminado)
            return NotFound(new { message = $"Categoría con Id {id} no encontrada." });

        return NoContent();
    }
}
