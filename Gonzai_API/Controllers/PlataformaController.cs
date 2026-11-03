using Gonzai_API.DTOs.Plataforma;
using Gonzai_API.Middleware;
using Gonzai_API.Services.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

/// <summary>
/// Alta y baja de empresas cliente. Cada empresa recibe su propia base de datos, así que
/// ninguna puede ver los clientes, ventas ni deudas de otra.
/// </summary>
[ApiController]
[Route("api/plataforma")]
[AllowAnonymous]
[ApiKeyPlataforma]
public class PlataformaController : ControllerBase
{
    private readonly ITenantProvisioningService _service;

    public PlataformaController(ITenantProvisioningService service)
    {
        _service = service;
    }

    // GET: api/plataforma/empresas
    [HttpGet("empresas")]
    public async Task<ActionResult<IEnumerable<EmpresaResponseDto>>> Listar(CancellationToken ct) =>
        Ok(await _service.ListarAsync(ct));

    // POST: api/plataforma/empresas
    [HttpPost("empresas")]
    public async Task<ActionResult<EmpresaResponseDto>> Crear([FromBody] EmpresaCreateDto dto, CancellationToken ct)
    {
        var empresa = await _service.CrearAsync(dto, ct);
        return CreatedAtAction(nameof(Listar), new { }, empresa);
    }

    // PATCH: api/plataforma/empresas/5/estado
    [HttpPatch("empresas/{id:int}/estado")]
    public async Task<ActionResult<EmpresaResponseDto>> CambiarEstado(
        int id, [FromBody] CambiarEstadoEmpresaDto dto, CancellationToken ct)
    {
        var empresa = await _service.CambiarEstadoAsync(id, dto.Activa, ct);

        if (empresa is null)
            return NotFound(new { message = $"No existe una empresa con Id {id}." });

        return Ok(empresa);
    }

    // POST: api/plataforma/migrar
    [HttpPost("migrar")]
    public async Task<IActionResult> Migrar(CancellationToken ct)
    {
        var empresas = await _service.MigrarTodasAsync(ct);
        return Ok(new { message = $"Migraciones al día en {empresas} empresa(s).", empresas });
    }
}

public class CambiarEstadoEmpresaDto
{
    public bool Activa { get; set; }
}
