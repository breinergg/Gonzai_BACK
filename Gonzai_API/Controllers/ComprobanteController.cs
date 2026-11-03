using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/comprobantes")]
public class ComprobanteController : ControllerBase
{
    private readonly IComprobantePublicoService _service;

    public ComprobanteController(IComprobantePublicoService service)
    {
        _service = service;
    }

    // Público: quien tenga el enlace descarga el PDF. El token vence a los 15 días.
    [AllowAnonymous]
    [HttpGet("{token}")]
    public async Task<IActionResult> Descargar(string token, CancellationToken ct)
    {
        var archivo = await _service.PorTokenAsync(token, ct);
        if (archivo is null)
            return NotFound(new { message = "El comprobante no existe o el enlace venció." });

        return File(archivo.Value.Pdf, "application/pdf", archivo.Value.NombreArchivo);
    }
}
