using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize]
public class ReportesController : ControllerBase
{
    private const string ExcelMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IExcelExportService _excel;

    public ReportesController(IExcelExportService excel)
    {
        _excel = excel;
    }

    // GET: api/reportes/libro-ventas?desde=2026-10-01&hasta=2026-10-31&clienteId=5&soloPendientes=false&incluirAnuladas=false
    // Sin fechas: mes actual (Colombia), del día 1 a hoy.
    [HttpGet("libro-ventas")]
    public async Task<IActionResult> LibroVentas(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? clienteId,
        [FromQuery] bool soloPendientes = false,
        [FromQuery] bool incluirAnuladas = false)
    {
        try
        {
            var archivo = await _excel.LibroVentasAsync(
                desde, hasta, clienteId, soloPendientes, incluirAnuladas, User.IsInRole("Admin"));
            return Archivo(archivo);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    // GET: api/reportes/cartera
    [HttpGet("cartera")]
    public async Task<IActionResult> Cartera()
    {
        try
        {
            return Archivo(await _excel.CarteraAsync());
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    // GET: api/reportes/estado-cuenta/5
    [HttpGet("estado-cuenta/{clienteId:int}")]
    public async Task<IActionResult> EstadoCuenta(int clienteId)
    {
        var archivo = await _excel.EstadoCuentaAsync(clienteId);

        if (archivo is null)
            return NotFound(new { message = $"Cliente con Id {clienteId} no encontrado." });

        return Archivo(archivo);
    }

    // GET: api/reportes/abonos?desde=2026-10-18&hasta=2026-10-18&clienteId=5&incluirAnulados=false
    // Sin fechas: solo el día de hoy (Colombia).
    [HttpGet("abonos")]
    public async Task<IActionResult> Abonos(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? clienteId,
        [FromQuery] bool incluirAnulados = false)
    {
        try
        {
            var archivo = await _excel.AbonosAsync(desde, hasta, clienteId, incluirAnulados);
            return Archivo(archivo);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    private FileContentResult Archivo(ArchivoExcel archivo) =>
        File(archivo.Contenido, ExcelMime, archivo.NombreArchivo);
}
