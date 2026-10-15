using System.Text.Json;
using Gonzai_API.DTOs.VentaCliente;
using Gonzai_API.Models;
using Gonzai_API.Services.AI.Helpers;
using Gonzai_API.Services.Interfaces;

namespace Gonzai_API.Services.AI.Functions;

public sealed class ConsultarVentasHandler : GeminiFunctionHandlerBase
{
    private const int MaxVentasDetalle = 20;

    private readonly IVentaClienteService _ventaService;

    public ConsultarVentasHandler(IVentaClienteService ventaService) =>
        _ventaService = ventaService;

    public override string FunctionName => "consultar_ventas";

    public override async Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default)
    {
        var clienteId      = FunctionArgsHelper.GetInt(args, "cliente_id");
        var soloPendientes = FunctionArgsHelper.GetBool(args, "solo_pendientes");
        var mes            = FunctionArgsHelper.GetInt(args, "mes");
        var anio           = FunctionArgsHelper.GetInt(args, "anio");

        DateOnly? desde = null, hasta = null;
        string? periodo = null;

        if (FunctionArgsHelper.GetString(args, "fecha") is { } fechaStr && DateOnly.TryParse(fechaStr, out var fecha))
        {
            desde = hasta = fecha;
            periodo = fechaStr;
        }
        else if (mes is >= 1 and <= 12 && anio is > 0)
        {
            desde   = new DateOnly(anio.Value, mes.Value, 1);
            hasta   = desde.Value.AddMonths(1).AddDays(-1);
            periodo = $"{mes:D2}/{anio}";
        }

        if (desde is null && clienteId is null && !soloPendientes)
            return Success(await _ventaService.GetResumenMensualAsync());

        var ventas = (await _ventaService.GetAllAsync(clienteId, desde, hasta, soloPendientes)).ToList();
        var activas = ventas.Where(v => v.Estado == EstadosDocumento.Activa).ToList();

        return Success(new
        {
            periodo,
            clienteId,
            cantidadVentas   = activas.Count,
            totalVendido     = activas.Sum(v => v.Total),
            totalCobrado     = activas.Sum(v => v.MontoPagado),
            totalPendiente   = activas.Sum(v => v.SaldoPendiente),
            gananciaEstimada = activas.Sum(v => v.Ganancia),
            ventas           = activas.Take(MaxVentasDetalle).Select(Resumir)
        }, new { limit = MaxVentasDetalle, anuladasExcluidas = ventas.Count - activas.Count });
    }

    private static object Resumir(VentaClienteResponseDto v) => new
    {
        v.Id,
        fecha   = v.Fecha.ToString("yyyy-MM-dd"),
        cliente = v.ClienteNombre,
        v.Total,
        v.MontoPagado,
        debe    = v.SaldoPendiente,
        v.EstadoPago,
        productos = v.Detalles.Select(d => new { d.ProductoNombre, d.Cantidad, d.PrecioVentaUnitario })
    };
}
