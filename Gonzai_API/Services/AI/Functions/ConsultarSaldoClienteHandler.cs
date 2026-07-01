using System.Text.Json;
using Gonzai_API.Services.AI.Helpers;
using Gonzai_API.Services.Interfaces;

namespace Gonzai_API.Services.AI.Functions;

public sealed class ConsultarSaldoClienteHandler : GeminiFunctionHandlerBase
{
    private const int MaxResultados = 10;

    private readonly IClienteService _clienteService;
    private readonly IMovimientoClienteService _movimientoClienteService;

    public ConsultarSaldoClienteHandler(
        IClienteService clienteService,
        IMovimientoClienteService movimientoClienteService)
    {
        _clienteService            = clienteService;
        _movimientoClienteService  = movimientoClienteService;
    }

    public override string FunctionName => "consultar_saldo_cliente";

    public override async Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default)
    {
        var clienteId = FunctionArgsHelper.GetInt(args, "cliente_id");
        var nombre    = FunctionArgsHelper.GetString(args, "nombre");

        if (clienteId is null && string.IsNullOrWhiteSpace(nombre))
            return Failure("PARAMETROS_INVALIDOS", "Debes indicar 'nombre' o 'cliente_id'.");

        if (clienteId is > 0)
            return await BuildSaldoResponseAsync(clienteId.Value, cancellationToken);

        var saldos = (await _clienteService.SearchSaldosByNombreAsync(nombre!, MaxResultados)).ToList();

        if (saldos.Count == 0)
            return Failure("CLIENTE_NO_ENCONTRADO", $"No se encontró ningún cliente activo con nombre '{nombre}'.");

        if (saldos.Count > 1)
        {
            return Failure(
                "MULTIPLES_RESULTADOS",
                $"Se encontraron {saldos.Count} clientes con nombre similar a '{nombre}'. Pide aclaración al usuario.",
                saldos.Select(s => new { s.ClienteId, s.Nombre, s.Saldo }));
        }

        return await BuildSaldoResponseAsync(saldos[0].ClienteId, cancellationToken);
    }

    private async Task<string> BuildSaldoResponseAsync(int clienteId, CancellationToken cancellationToken)
    {
        var saldo = await _clienteService.GetSaldoByClienteIdAsync(clienteId);

        if (saldo is null)
            return Failure("CLIENTE_NO_ENCONTRADO", $"No se encontró el cliente con Id {clienteId}.");

        var movimientos = (await _movimientoClienteService.GetByClienteIdAsync(clienteId)).ToList();

        return Success(new
        {
            cliente = new { saldo.ClienteId, saldo.Nombre },
            balance = new
            {
                saldo.TotalDeuda,
                saldo.TotalAbonos,
                saldo.Saldo,
                enPazYSalvo = saldo.EnPazYSalvo
            },
            ultimosMovimientos = movimientos
                .Take(5)
                .Select(m => new
                {
                    m.TipoMovimiento,
                    m.Valor,
                    fecha = m.Fecha.ToString("yyyy-MM-dd HH:mm"),
                    m.Descripcion
                })
        }, new { totalMovimientos = movimientos.Count });
    }
}
