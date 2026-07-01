using System.Text.Json;
using Gonzai_API.Services.AI.Helpers;
using Gonzai_API.Services.Interfaces;

namespace Gonzai_API.Services.AI.Functions;

public sealed class ConsultarMovimientosClienteHandler : GeminiFunctionHandlerBase
{
    private readonly IClienteService _clienteService;
    private readonly IMovimientoClienteService _movimientoClienteService;

    public ConsultarMovimientosClienteHandler(
        IClienteService clienteService,
        IMovimientoClienteService movimientoClienteService)
    {
        _clienteService           = clienteService;
        _movimientoClienteService = movimientoClienteService;
    }

    public override string FunctionName => "consultar_movimientos_cliente";

    public override async Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default)
    {
        var clienteId = FunctionArgsHelper.GetInt(args, "cliente_id");

        if (clienteId is null or <= 0)
            return Failure("PARAMETROS_INVALIDOS", "El parámetro 'cliente_id' es requerido y debe ser un entero positivo.");

        var saldo = await _clienteService.GetSaldoByClienteIdAsync(clienteId.Value);

        if (saldo is null)
            return Failure("CLIENTE_NO_ENCONTRADO", $"No se encontró el cliente con Id {clienteId}.");

        var movimientos = (await _movimientoClienteService.GetByClienteIdAsync(clienteId.Value)).ToList();

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
            movimientos = movimientos.Select(m => new
            {
                m.Id,
                m.TipoMovimiento,
                m.Valor,
                fecha = m.Fecha.ToString("yyyy-MM-dd HH:mm"),
                m.Descripcion
            })
        }, new { totalMovimientos = movimientos.Count });
    }
}
