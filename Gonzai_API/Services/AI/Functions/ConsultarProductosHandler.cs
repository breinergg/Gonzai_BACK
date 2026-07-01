using System.Text.Json;
using Gonzai_API.Services.AI.Helpers;
using Gonzai_API.Services.Interfaces;

namespace Gonzai_API.Services.AI.Functions;

public sealed class ConsultarProductosHandler : GeminiFunctionHandlerBase
{
    private const int MaxResultados = 20;

    private readonly IProductoService _productoService;

    public ConsultarProductosHandler(IProductoService productoService) =>
        _productoService = productoService;

    public override string FunctionName => "consultar_productos";

    public override async Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default)
    {
        var nombre      = FunctionArgsHelper.GetString(args, "nombre");
        var soloActivos = FunctionArgsHelper.GetBool(args, "solo_activos", defaultValue: true);

        var productos = (await _productoService.SearchAsync(nombre, soloActivos, MaxResultados)).ToList();

        return Success(new
        {
            total     = productos.Count,
            productos = productos.Select(p => new
            {
                p.Id,
                p.Nombre,
                categoria = p.CategoriaNombre,
                p.PrecioVenta,
                p.PrecioCompra,
                p.StockActual
            })
        }, new { limit = MaxResultados, soloActivos });
    }
}
