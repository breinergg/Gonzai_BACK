using System.Text.Json;
using Gonzai_API.Services.AI.Helpers;
using Gonzai_API.Services.Interfaces;

namespace Gonzai_API.Services.AI.Functions;

public sealed class ConsultarClientesHandler : GeminiFunctionHandlerBase
{
    private const int MaxResultados = 20;

    private readonly IClienteService _clienteService;

    public ConsultarClientesHandler(IClienteService clienteService) =>
        _clienteService = clienteService;

    public override string FunctionName => "consultar_clientes";

    public override async Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default)
    {
        var filtro   = FunctionArgsHelper.GetString(args, "filtro");
        var conDeuda = FunctionArgsHelper.GetBool(args, "con_deuda");

        var clientes = string.IsNullOrWhiteSpace(filtro)
            ? (await _clienteService.GetAllActivosAsync()).Take(MaxResultados).ToList()
            : (await _clienteService.SearchActivosByNombreAsync(filtro, MaxResultados)).ToList();

        if (conDeuda)
        {
            return Success(new
            {
                totalClientes      = clientes.Count,
                clientesConDeuda   = await _clienteService.GetClientesActivosConDeudaCountAsync(),
                totalDeudaGeneral  = await _clienteService.GetTotalDeudaClientesActivosAsync(),
                mayorDeudor        = await _clienteService.GetClienteConMayorDeudaAsync(),
                clientes           = clientes.Select(c => new { c.Id, c.Nombre, c.Telefono })
            }, new { limit = MaxResultados });
        }

        return Success(new
        {
            total    = clientes.Count,
            clientes = clientes.Select(c => new { c.Id, c.Nombre, c.Telefono, c.Direccion })
        }, new { limit = MaxResultados });
    }
}
