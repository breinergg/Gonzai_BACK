using System.Net;
using System.Net.Http.Json;
using Gonzai_API.Models;

namespace Gonzai_API.Tests;

[Collection(ApiCollection.Nombre)]
public class VentasTests
{
    private readonly ApiFixture _api;
    public VentasTests(ApiFixture api) => _api = api;

    private async Task<Escenario> NuevoAsync() => new(await _api.AdminAsync());

    [Fact]
    public async Task Venta_a_credito_descuenta_stock_y_deja_todo_pendiente()
    {
        var e = await NuevoAsync();
        var cliente = await e.CrearClienteAsync();
        var variante = await e.CrearVarianteAsync(stock: 10);

        var venta = await e.VenderOkAsync(cliente.Id, variante.Id, cantidad: 3);

        Assert.Equal(3 * Escenario.Precio, venta.Total);
        Assert.Equal(0, venta.MontoPagado);
        Assert.Equal(venta.Total, venta.SaldoPendiente);
        Assert.Equal(EstadosPagoVenta.Pendiente, venta.EstadoPago);
        Assert.Equal(3 * Escenario.Costo, venta.CostoTotal);
        Assert.Equal(7, await e.StockAsync(variante.ProductoId));
        Assert.Equal(venta.Total, await e.SaldoClienteAsync(cliente.Id));
    }

    [Fact]
    public async Task Venta_con_pago_parcial_queda_Parcial()
    {
        var e = await NuevoAsync();
        var cliente = await e.CrearClienteAsync();
        var variante = await e.CrearVarianteAsync(10);

        var venta = await e.VenderOkAsync(cliente.Id, variante.Id, 2, pagado: 5_000m, metodo: "efectivo");

        Assert.Equal(EstadosPagoVenta.Parcial, venta.EstadoPago);
        Assert.Equal(15_000m, venta.SaldoPendiente);
        Assert.Equal(15_000m, await e.SaldoClienteAsync(cliente.Id));
    }

    [Fact]
    public async Task Venta_pagada_completa_no_deja_saldo()
    {
        var e = await NuevoAsync();
        var cliente = await e.CrearClienteAsync();
        var variante = await e.CrearVarianteAsync(10);

        var venta = await e.VenderOkAsync(cliente.Id, variante.Id, 1, pagado: Escenario.Precio, metodo: "nequi");

        Assert.Equal(EstadosPagoVenta.Pagada, venta.EstadoPago);
        Assert.Equal(0, venta.SaldoPendiente);
        Assert.Equal(0, await e.SaldoClienteAsync(cliente.Id));
    }

    [Fact]
    public async Task Venta_sin_stock_suficiente_se_rechaza_y_no_toca_el_inventario()
    {
        var e = await NuevoAsync();
        var cliente = await e.CrearClienteAsync();
        var variante = await e.CrearVarianteAsync(2);

        var r = await e.VenderAsync(cliente.Id, variante.Id, cantidad: 3);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Equal(2, await e.StockAsync(variante.ProductoId));
        Assert.Equal(0, await e.SaldoClienteAsync(cliente.Id));
    }

    [Fact]
    public async Task Venta_con_pago_sin_metodo_se_rechaza()
    {
        var e = await NuevoAsync();
        var cliente = await e.CrearClienteAsync();
        var variante = await e.CrearVarianteAsync(5);

        var r = await e.VenderAsync(cliente.Id, variante.Id, 1, pagado: 1_000m, metodo: null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Equal(5, await e.StockAsync(variante.ProductoId));
    }

    [Fact]
    public async Task Venta_con_pago_mayor_al_total_se_rechaza()
    {
        var e = await NuevoAsync();
        var cliente = await e.CrearClienteAsync();
        var variante = await e.CrearVarianteAsync(5);

        var r = await e.VenderAsync(cliente.Id, variante.Id, 1, pagado: Escenario.Precio + 1, metodo: "efectivo");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Equal(5, await e.StockAsync(variante.ProductoId));
    }

    [Fact]
    public async Task Venta_con_cantidad_cero_es_invalida()
    {
        var e = await NuevoAsync();
        var cliente = await e.CrearClienteAsync();
        var variante = await e.CrearVarianteAsync(5);

        var r = await e.VenderAsync(cliente.Id, variante.Id, cantidad: 0);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Ventas_simultaneas_de_la_ultima_unidad_solo_dejan_pasar_una()
    {
        var e = await NuevoAsync();
        var cliente = await e.CrearClienteAsync();
        var variante = await e.CrearVarianteAsync(stock: 1);

        var respuestas = await Task.WhenAll(Enumerable.Range(0, 6)
            .Select(_ => e.VenderAsync(cliente.Id, variante.Id, 1)));

        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(5, respuestas.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity));
        Assert.Equal(0, await e.StockAsync(variante.ProductoId));
        Assert.Equal(Escenario.Precio, await e.SaldoClienteAsync(cliente.Id));
    }

    [Fact]
    public async Task Anular_venta_devuelve_el_stock_y_limpia_la_deuda()
    {
        var e = await NuevoAsync();
        var cliente = await e.CrearClienteAsync();
        var variante = await e.CrearVarianteAsync(10);
        var venta = await e.VenderOkAsync(cliente.Id, variante.Id, 4, pagado: 10_000m, metodo: "efectivo");
        Assert.Equal(6, await e.StockAsync(variante.ProductoId));

        var r = await e.AnularVentaAsync(venta.Id);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(EstadosDocumento.Anulada, (await e.VentaAsync(venta.Id)).Estado);
        Assert.Equal(10, await e.StockAsync(variante.ProductoId));
        Assert.Equal(0, await e.SaldoClienteAsync(cliente.Id));
    }

    [Fact]
    public async Task Anular_venta_dos_veces_se_rechaza_y_no_duplica_el_stock()
    {
        var e = await NuevoAsync();
        var cliente = await e.CrearClienteAsync();
        var variante = await e.CrearVarianteAsync(10);
        var venta = await e.VenderOkAsync(cliente.Id, variante.Id, 4);

        await e.AnularVentaAsync(venta.Id);
        var segunda = await e.AnularVentaAsync(venta.Id);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, segunda.StatusCode);
        Assert.Equal(10, await e.StockAsync(variante.ProductoId));
    }

    [Fact]
    public async Task Compra_a_proveedor_suma_stock_y_anularla_lo_retira()
    {
        var e = await NuevoAsync();
        var variante = await e.CrearVarianteAsync(stock: 0);

        var compra = await e.ComprarAsync(variante.Id, cantidad: 8);
        Assert.Equal(8, await e.StockAsync(variante.ProductoId));
        Assert.Equal(8 * Escenario.Costo, compra.SubtotalProductos);

        var r = await e.Admin.PostAsJsonAsync($"/api/compraproveedor/{compra.Id}/anular", new { motivo = "Prueba" });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(0, await e.StockAsync(variante.ProductoId));
    }

    [Fact]
    public async Task Una_venta_consume_primero_el_lote_mas_antiguo_FIFO()
    {
        var e = await NuevoAsync();
        var cliente = await e.CrearClienteAsync();
        var variante = await e.CrearVarianteAsync(stock: 0);
        var primero = await e.ComprarAsync(variante.Id, cantidad: 2);
        await e.ComprarAsync(variante.Id, cantidad: 5);

        var venta = await e.VenderOkAsync(cliente.Id, variante.Id, cantidad: 2);

        var consumo = venta.Detalles.Single().Consumos.Single();
        Assert.Equal(primero.Id, consumo.CompraProveedorId);
        Assert.Equal(5, await e.StockAsync(variante.ProductoId));
    }
}
