using Gonzai_API.DTOs.Negocio;
using Gonzai_API.Services.Email;
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
    private readonly IEmailSender _email;

    public NegocioController(INegocioService service, IEmailSender email)
    {
        _service = service;
        _email   = email;
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

    /// <summary>
    /// Configura la clave de Gemini propia de la empresa. A partir de aquí, el asistente de IA
    /// la usa en vez de la clave compartida de la plataforma y deja de contar para su cupo diario.
    /// </summary>
    [HttpPut("ia/gemini")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<NegocioResponseDto>> ConfigurarClaveGemini([FromBody] NegocioGeminiApiKeyDto dto) =>
        Ok(await _service.ConfigurarClaveGeminiAsync(dto.ApiKey));

    /// <summary>Quita la clave propia: el asistente vuelve a usar la clave compartida de la plataforma.</summary>
    [HttpDelete("ia/gemini")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<NegocioResponseDto>> QuitarClaveGemini() =>
        Ok(await _service.QuitarClaveGeminiAsync());

    /// <summary>
    /// Configura el correo propio de la empresa para enviar comprobantes. A partir de aquí, los
    /// envíos cuentan para su propio límite diario, no el de la plataforma.
    /// </summary>
    [HttpPut("smtp")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<NegocioResponseDto>> ConfigurarSmtp([FromBody] NegocioSmtpDto dto) =>
        Ok(await _service.ConfigurarSmtpAsync(dto));

    /// <summary>Quita el correo propio: los comprobantes vuelven a enviarse con el correo compartido.</summary>
    [HttpDelete("smtp")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<NegocioResponseDto>> QuitarSmtp() =>
        Ok(await _service.QuitarSmtpAsync());

    /// <summary>Manda un correo corto a la propia empresa, para confirmar que la configuración funciona.</summary>
    [HttpPost("smtp/probar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ProbarSmtp()
    {
        var negocio = await _service.GetAsync();

        if (!negocio.TieneSmtpPropio || string.IsNullOrWhiteSpace(negocio.SmtpUsuario))
            return UnprocessableEntity(new { message = "Guarda primero tu correo antes de probarlo." });

        try
        {
            await _email.EnviarPruebaAsync(negocio.SmtpUsuario, negocio.NombreComercial);
            return Ok(new { message = $"Correo de prueba enviado a {negocio.SmtpUsuario}. Revisa tu bandeja." });
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(new
            {
                message = "No se pudo enviar. Revisa el correo y la contraseña de aplicación.",
                detalle = ex.Message
            });
        }
    }
}
