using System.Text.Json;
using Gonzai_API.Services.AI.Helpers;
using Gonzai_API.Services.Interfaces;

namespace Gonzai_API.Services.AI.Functions;

public sealed class ConsultarVentasDiariasHandler : GeminiFunctionHandlerBase
{
    private readonly IVentaDiariaService _ventaDiariaService;

    public ConsultarVentasDiariasHandler(IVentaDiariaService ventaDiariaService) =>
        _ventaDiariaService = ventaDiariaService;

    public override string FunctionName => "consultar_ventas_diarias";

    public override async Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default)
    {
        if (FunctionArgsHelper.GetString(args, "fecha") is { } fechaStr
            && DateOnly.TryParse(fechaStr, out var fecha))
        {
            var ventas = (await _ventaDiariaService.GetByFechaAsync(fecha)).ToList();
            return Success(new
            {
                fecha       = fechaStr,
                registros   = ventas.Count,
                totalVentas = ventas.Sum(v => v.Total),
                ventas      = ventas.Select(v => new { v.Id, v.Total, v.Descripcion })
            });
        }

        var mes  = FunctionArgsHelper.GetInt(args, "mes");
        var anio = FunctionArgsHelper.GetInt(args, "anio");

        if (mes is >= 1 and <= 12 && anio is > 0)
        {
            var desde  = new DateOnly(anio.Value, mes.Value, 1);
            var hasta  = desde.AddMonths(1).AddDays(-1);
            var ventas = (await _ventaDiariaService.GetByRangoFechaAsync(desde, hasta)).ToList();

            return Success(new
            {
                periodo     = $"{mes:D2}/{anio}",
                registros   = ventas.Count,
                totalVentas = ventas.Sum(v => v.Total),
                ventas      = ventas.Select(v => new { v.Fecha, v.Total, v.Descripcion })
            });
        }

        var resumen = await _ventaDiariaService.GetResumenMensualAsync();
        return Success(resumen);
    }
}
