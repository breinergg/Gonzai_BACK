using System.Net;
using Gonzai_API.Models;

namespace Gonzai_API.Tests;

[Collection(ApiCollection.Nombre)]
public class PagosTests
{
    private readonly ApiFixture _api;
    public PagosTests(ApiFixture api) => _api = api;

    private async Task<(Escenario e, int clienteId, int ventaId)> VentaDe20milAsync()
    {
        var e = new Escenario(await _api.AdminAsync());
        var cliente = await e.CrearClienteAsync();
        var variante = await e.CrearVarianteAsync(10);
        var venta = await e.VenderOkAsync(cliente.Id, variante.Id, cantidad: 2);
        return (e, cliente.Id, venta.Id);
    }

    [Fact]
    public async Task Pagos_parciales_reducen_el_saldo_hasta_dejar_la_venta_Pagada()
    {
        var (e, clienteId, ventaId) = await VentaDe20milAsync();

        var primero = await e.PagarOkAsync(ventaId, clienteId, 8_000m);
        Assert.Equal(12_000m, primero.SaldoPendienteVenta);
        Assert.Equal(EstadosPagoVenta.Parcial, (await e.VentaAsync(ventaId)).EstadoPago);

        await e.PagarOkAsync(ventaId, clienteId, 12_000m);
        var venta = await e.VentaAsync(ventaId);
        Assert.Equal(EstadosPagoVenta.Pagada, venta.EstadoPago);
        Assert.Equal(0, venta.SaldoPendiente);
        Assert.Equal(0, await e.SaldoClienteAsync(clienteId));
    }

    [Fact]
    public async Task Pago_mayor_al_saldo_se_rechaza()
    {
        var (e, clienteId, ventaId) = await VentaDe20milAsync();

        var r = await e.PagarAsync(ventaId, clienteId, 20_001m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Equal(20_000m, (await e.VentaAsync(ventaId)).SaldoPendiente);
    }

    [Fact]
    public async Task Pagar_una_venta_ya_pagada_se_rechaza()
    {
        var (e, clienteId, ventaId) = await VentaDe20milAsync();
        await e.PagarOkAsync(ventaId, clienteId, 20_000m);

        var r = await e.PagarAsync(ventaId, clienteId, 1_000m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
    }

    [Fact]
    public async Task Pago_de_otro_cliente_se_rechaza()
    {
        var (e, _, ventaId) = await VentaDe20milAsync();
        var otro = await e.CrearClienteAsync();

        var r = await e.PagarAsync(ventaId, otro.Id, 1_000m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
    }

    [Fact]
    public async Task Pago_con_monto_cero_o_negativo_es_invalido()
    {
        var (e, clienteId, ventaId) = await VentaDe20milAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await e.PagarAsync(ventaId, clienteId, 0)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.PagarAsync(ventaId, clienteId, -5m)).StatusCode);
    }

    [Fact]
    public async Task Anular_un_pago_restaura_el_saldo_de_la_venta_y_del_cliente()
    {
        var (e, clienteId, ventaId) = await VentaDe20milAsync();
        var pago = await e.PagarOkAsync(ventaId, clienteId, 20_000m);
        Assert.Equal(0, await e.SaldoClienteAsync(clienteId));

        var r = await e.AnularPagoAsync(pago.Id);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var venta = await e.VentaAsync(ventaId);
        Assert.Equal(20_000m, venta.SaldoPendiente);
        Assert.Equal(EstadosPagoVenta.Pendiente, venta.EstadoPago);
        Assert.Equal(20_000m, await e.SaldoClienteAsync(clienteId));
    }

    [Fact]
    public async Task Anular_un_pago_dos_veces_se_rechaza()
    {
        var (e, clienteId, ventaId) = await VentaDe20milAsync();
        var pago = await e.PagarOkAsync(ventaId, clienteId, 5_000m);

        await e.AnularPagoAsync(pago.Id);
        var segunda = await e.AnularPagoAsync(pago.Id);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, segunda.StatusCode);
        Assert.Equal(20_000m, (await e.VentaAsync(ventaId)).SaldoPendiente);
    }

    [Fact]
    public async Task No_se_puede_pagar_una_venta_anulada()
    {
        var (e, clienteId, ventaId) = await VentaDe20milAsync();
        await e.AnularVentaAsync(ventaId);

        var r = await e.PagarAsync(ventaId, clienteId, 1_000m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
    }

    [Fact]
    public async Task Pagos_simultaneos_que_exceden_el_saldo_no_sobrepagan()
    {
        var (e, clienteId, ventaId) = await VentaDe20milAsync();

        // 4 pagos de 8.000 sobre una deuda de 20.000: caben como máximo 2.
        var respuestas = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => e.PagarAsync(ventaId, clienteId, 8_000m)));

        Assert.Equal(2, respuestas.Count(r => r.StatusCode == HttpStatusCode.Created));
        var venta = await e.VentaAsync(ventaId);
        Assert.Equal(16_000m, venta.MontoPagado);
        Assert.Equal(4_000m, venta.SaldoPendiente);
    }
}
