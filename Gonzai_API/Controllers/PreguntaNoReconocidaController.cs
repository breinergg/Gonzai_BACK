using Gonzai_API.DTOs.PreguntaNoReconocida;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PreguntaNoReconocidaController : ControllerBase
{
    private readonly IPreguntaNoReconocidaService _service;

    public PreguntaNoReconocidaController(IPreguntaNoReconocidaService service)
    {
        _service = service;
    }

    // GET: api/preguntanoreconocida
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<PreguntaNoReconocidaResponseDto>>> GetAll()
    {
        var preguntas = await _service.GetAllAsync();
        return Ok(preguntas);
    }

    // POST: api/preguntanoreconocida
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<PreguntaNoReconocidaResponseDto>> Create([FromBody] PreguntaNoReconocidaCreateDto dto)
    {
        var pregunta = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetAll), pregunta);
    }
}
