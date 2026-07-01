using System.Text.Json;
using Gonzai_API.Services.AI.Helpers;
using Gonzai_API.Services.Interfaces;

namespace Gonzai_API.Services.AI.Functions;

public sealed class ConsultarInventarioHandler : GeminiFunctionHandlerBase
{
    private const int MaxResultados = 50;

    private readonly IMovimientoInventarioService _movimientoInventarioService;

    public ConsultarInventarioHandler(IMovimientoInventarioService movimientoInventarioService) =>
        _movimientoInventarioService = movimientoInventarioService;

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

        return Success(new
        {
            total       = movimientos.Count,
            movimientos = movimientos.Select(m => new
            {
                m.Id,
                producto = m.ProductoNombre,
                m.TipoMovimiento,
                m.Cantidad,
                fecha = m.Fecha.ToString("yyyy-MM-dd HH:mm"),
                m.Descripcion
            })
        }, new { limit = MaxResultados, productoId, tipo });
    }
}
