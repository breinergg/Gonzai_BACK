using System.Text.Json;
using Gonzai_API.Services.AI.Helpers;
using Gonzai_API.Services.Interfaces;

namespace Gonzai_API.Services.AI.Functions;

public sealed class ConsultarInventarioHandler : GeminiFunctionHandlerBase
{
    private const int MaxResultados = 50;

    private readonly IMovimientoInventarioService _movimientoInventarioService;
    private readonly ILoteService _loteService;

    public ConsultarInventarioHandler(
        IMovimientoInventarioService movimientoInventarioService,
        ILoteService loteService)
    {
        _movimientoInventarioService = movimientoInventarioService;
        _loteService                 = loteService;
    }

    public override string FunctionName => "consultar_inventario";

    public override async Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default)
    {
        var productoId = FunctionArgsHelper.GetInt(args, "producto_id");
        var tipo       = FunctionArgsHelper.GetString(args, "tipo");

        if (!string.IsNullOrWhiteSpace(tipo)
            && !tipo.Equals("entrada", StringComparison.OrdinalIgnoreCase)
            && !tipo.Equals("salida", StringComparison.OrdinalIgnoreCase))
        {
            return Failure("PARAMETROS_INVALIDOS", "El parámetro 'tipo' debe ser 'entrada' o 'salida'.");
        }

        var movimientos = (await _movimientoInventarioService.GetRecentAsync(
            productoId, tipo, MaxResultados)).ToList();

        var lotes = productoId is > 0
            ? (await _loteService.GetDisponiblesAsync(productoId.Value, null)).Select(e => new
            {
                variante  = e.VarianteNombre,
                lote      = e.LoteCodigo ?? e.Origen,
                e.CantidadDisponible,
                e.CostoUnitario,
                ingreso   = e.FechaIngreso.ToString("yyyy-MM-dd"),
                proveedor = e.ProveedorNombre
            }).ToList<object>()
            : null;

        return Success(new
        {
            lotesDisponibles = lotes,
            total            = movimientos.Count,
            movimientos      = movimientos.Select(m => new
            {
                m.Id,
                producto = m.VarianteNombre,
                m.TipoMovimiento,
                m.Motivo,
                m.Cantidad,
                lote = m.LoteCodigo,
                m.StockResultante,
                fecha = m.Fecha.ToString("yyyy-MM-dd HH:mm"),
                m.Descripcion
            })
        }, new { limit = MaxResultados, productoId, tipo });
    }
}
