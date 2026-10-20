using Gonzai_API.DTOs.Negocio;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/negocio")]
[Authorize]
public class NegocioController : ControllerBase
{
    private readonly INegocioService _service;

    public NegocioController(INegocioService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<NegocioResponseDto>> Get() => Ok(await _service.GetAsync());

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<NegocioResponseDto>> Update([FromBody] NegocioUpdateDto dto)
    {
        try
        {
            return Ok(await _service.UpdateAsync(dto));
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    [HttpGet("logo")]
    public async Task<IActionResult> Logo()
    {
        var logo = await _service.GetLogoAsync();
        if (logo is null || logo.Length == 0)
            return NotFound(new { message = "El negocio no tiene logo." });

        return File(logo, "image/png");
    }

    /// <summary>PNG, máximo 500 KB y 2000 px por lado. Recomendado: 600×200 o 400×400.</summary>
    [HttpPut("logo")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(600_000)]
    public async Task<ActionResult<NegocioResponseDto>> SubirLogo(IFormFile archivo)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { message = "Adjunta el logo en el campo 'archivo'." });

        try
        {
            await using var memoria = new MemoryStream();
            await archivo.CopyToAsync(memoria);
            return Ok(await _service.UpdateLogoAsync(memoria.ToArray()));
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    [HttpDelete("logo")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<NegocioResponseDto>> QuitarLogo() => Ok(await _service.QuitarLogoAsync());
}
