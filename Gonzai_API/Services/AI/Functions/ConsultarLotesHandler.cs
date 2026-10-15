using System.Text.Json;
using Gonzai_API.DTOs.Lote;
using Gonzai_API.Services.AI.Helpers;
using Gonzai_API.Services.Interfaces;

namespace Gonzai_API.Services.AI.Functions;

public sealed class ConsultarLotesHandler : GeminiFunctionHandlerBase
{
    private const int MaxResultados = 20;

    private readonly ILoteService _loteService;

    public ConsultarLotesHandler(ILoteService loteService) =>
        _loteService = loteService;

    public override string FunctionName => "consultar_lotes";

    public override async Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default)
    {
        var codigo       = FunctionArgsHelper.GetString(args, "codigo");
        var loteId       = FunctionArgsHelper.GetInt(args, "lote_id");
        var soloConStock = FunctionArgsHelper.GetBool(args, "solo_con_stock", defaultValue: false);

        if (!string.IsNullOrWhiteSpace(codigo) || loteId is > 0)
        {
            var lote = !string.IsNullOrWhiteSpace(codigo)
                ? await _loteService.GetByCodigoAsync(codigo)
                : await _loteService.GetByIdAsync(loteId!.Value);

            if (lote is null)
                return Failure("LOTE_NO_ENCONTRADO", $"No se encontró el lote {codigo ?? loteId.ToString()}.");

            return Success(new
            {
                lote = Resumen(lote),
                prendas = lote.Lineas.Select(l => new
                {
                    prenda = l.VarianteNombre,
                    l.CostoUnitario,
                    l.Comprado,
                    l.Vendido,
                    l.Disponible,
                    l.Ajustado,
                    l.Ingresos,
                    l.GananciaBruta
                })
            });
        }

        var lotes = (await _loteService.GetAllAsync(soloConStock: soloConStock)).Take(MaxResultados).ToList();

        return Success(new
        {
            total = lotes.Count,
            lotes = lotes.Select(Resumen)
        }, new { limit = MaxResultados, soloConStock });
    }

    private static object Resumen(LoteResumenDto l) => new
    {
        loteId = l.CompraProveedorId,
        l.Codigo,
        l.Nombre,
        proveedor = l.ProveedorNombre,
        fecha     = l.Fecha.ToString("yyyy-MM-dd"),
        l.Estado,
        l.UnidadesCompradas,
        l.UnidadesVendidas,
        l.UnidadesDisponibles,
        l.SubtotalProductos,
        l.CostoEnvio,
        l.InversionTotal,
        l.Ingresos,
        l.GananciaBruta,
        l.GananciaNeta,
        l.PorcentajeVendido,
        l.PorcentajeRecuperado,
        l.InversionRecuperada
    };
}
