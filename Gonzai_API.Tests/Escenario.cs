using System.Net;
using System.Net.Http.Json;
using Gonzai_API.DTOs.Cliente;
using Gonzai_API.DTOs.CompraProveedor;
using Gonzai_API.DTOs.PagoCliente;
using Gonzai_API.DTOs.Producto;
using Gonzai_API.DTOs.Proveedor;
using Gonzai_API.DTOs.VentaCliente;

namespace Gonzai_API.Tests;

/// <summary>Atajos para armar datos de prueba por la API, con nombres únicos para que los tests no se pisen.</summary>
public class Escenario
{
    public const decimal Precio = 10_000m;
    public const decimal Costo  = 5_000m;

    public HttpClient Admin { get; }

    public Escenario(HttpClient admin) => Admin = admin;

    private static string Unico(string prefijo) => $"{prefijo}-{Guid.NewGuid():N}"[..20];

    public async Task<ClienteResponseDto> CrearClienteAsync()
    {
        var r = await Admin.PostAsJsonAsync("/api/cliente", new ClienteCreateDto { Nombre = Unico("Cliente") });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ClienteResponseDto>())!;
    }

    /// <summary>Producto con una sola variante y el stock indicado.</summary>
    public async Task<ProductoVarianteResponseDto> CrearVarianteAsync(int stock)
    {
        var r = await Admin.PostAsJsonAsync("/api/producto", new ProductoCreateDto
        {
            Nombre = Unico("Producto"), PrecioVenta = Precio, StockInicial = stock, CostoInicial = Costo
        });
        r.EnsureSuccessStatusCode();
        var producto = (await r.Content.ReadFromJsonAsync<ProductoResponseDto>())!;
        return producto.Variantes.Single();
    }

    public async Task<int> StockAsync(int productoId)
    {
        var producto = await Admin.GetFromJsonAsync<ProductoResponseDto>($"/api/producto/{productoId}");
        return producto!.Variantes.Sum(v => v.StockActual);
    }

    public Task<HttpResponseMessage> VenderAsync(int clienteId, int varianteId, int cantidad,
        decimal pagado = 0, string? metodo = null) =>
        Admin.PostAsJsonAsync("/api/ventacliente", new VentaClienteCreateDto
        {
            ClienteId = clienteId,
            MontoPagado = pagado,
            MetodoPago = metodo,
            Detalles = [new VentaClienteDetalleCreateDto
            {
                ProductoVarianteId = varianteId, Cantidad = cantidad, PrecioVentaUnitario = Precio
            }]
        });

    public async Task<VentaClienteResponseDto> VenderOkAsync(int clienteId, int varianteId, int cantidad,
        decimal pagado = 0, string? metodo = null)
    {
        var r = await VenderAsync(clienteId, varianteId, cantidad, pagado, metodo);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<VentaClienteResponseDto>())!;
    }

    public Task<HttpResponseMessage> PagarAsync(int ventaId, int clienteId, decimal monto) =>
        Admin.PostAsJsonAsync("/api/pagocliente", new PagoClienteCreateDto
        {
            VentaClienteId = ventaId, ClienteId = clienteId, Monto = monto, MetodoPago = "efectivo"
        });

    public async Task<PagoClienteResponseDto> PagarOkAsync(int ventaId, int clienteId, decimal monto)
    {
        var r = await PagarAsync(ventaId, clienteId, monto);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<PagoClienteResponseDto>())!;
    }

    public async Task<VentaClienteResponseDto> VentaAsync(int id) =>
        (await Admin.GetFromJsonAsync<VentaClienteResponseDto>($"/api/ventacliente/{id}"))!;

    public async Task<decimal> SaldoClienteAsync(int clienteId) =>
        (await Admin.GetFromJsonAsync<ClienteSaldoDto>($"/api/cliente/{clienteId}/saldo"))!.Saldo;

    public Task<HttpResponseMessage> AnularVentaAsync(int id) =>
        Admin.PostAsJsonAsync($"/api/ventacliente/{id}/anular", new { motivo = "Prueba automatizada" });

    public Task<HttpResponseMessage> AnularPagoAsync(int id) =>
        Admin.PostAsJsonAsync($"/api/pagocliente/{id}/anular", new { motivo = "Prueba automatizada" });

    public async Task<CompraProveedorResponseDto> ComprarAsync(int varianteId, int cantidad)
    {
        var p = await Admin.PostAsJsonAsync("/api/proveedor", new ProveedorCreateDto { Nombre = Unico("Proveedor") });
        p.EnsureSuccessStatusCode();
        var proveedor = (await p.Content.ReadFromJsonAsync<ProveedorResponseDto>())!;

        var r = await Admin.PostAsJsonAsync("/api/compraproveedor", new CompraProveedorCreateDto
        {
            ProveedorId = proveedor.Id,
            Detalles = [new CompraProveedorDetalleCreateDto
            {
                ProductoVarianteId = varianteId, Cantidad = cantidad, CostoUnitario = Costo
            }]
        });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<CompraProveedorResponseDto>())!;
    }
}
