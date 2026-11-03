using System.Net;
using System.Net.Http.Json;
using Gonzai_API.DTOs.Usuario;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Gonzai_API.Tests;

[Collection(ApiCollection.Nombre)]
public class SeguridadYOperacionTests
{
    private readonly ApiFixture _api;
    public SeguridadYOperacionTests(ApiFixture api) => _api = api;

    [Fact]
    public async Task Health_responde_200_y_no_requiere_login()
    {
        var r = await _api.Anonimo().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Theory]
    [InlineData("/api/cliente")]
    [InlineData("/api/producto")]
    [InlineData("/api/ventacliente")]
    [InlineData("/api/pagocliente/1")]
    [InlineData("/api/compraproveedor")]
    public async Task Los_endpoints_de_negocio_exigen_token(string ruta)
    {
        var r = await _api.Anonimo().GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Login_con_credenciales_malas_devuelve_401()
    {
        var r = await _api.Anonimo().PostAsJsonAsync("/api/usuario/login",
            new LoginDto { Email = ApiFixture.AdminEmail, Password = "incorrecta" });

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Un_vendedor_no_puede_anular_ventas_pero_un_admin_si()
    {
        var admin = await _api.AdminAsync();
        var e = new Escenario(admin);

        var email = $"vendedor-{Guid.NewGuid():N}@gonzai.local";
        var crear = await admin.PostAsJsonAsync("/api/usuario", new UsuarioCreateDto
        {
            Nombre = "Vendedor Prueba", Email = email, Password = "Vendedor-123", Rol = "Vendedor"
        });
        crear.EnsureSuccessStatusCode();
        var vendedor = await _api.ConTokenAsync(email, "Vendedor-123");

        var cliente = await e.CrearClienteAsync();
        var variante = await e.CrearVarianteAsync(5);
        var venta = await e.VenderOkAsync(cliente.Id, variante.Id, 1);

        var anularComoVendedor = await vendedor.PostAsJsonAsync($"/api/ventacliente/{venta.Id}/anular", new { motivo = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, anularComoVendedor.StatusCode);
        Assert.Equal(4, await e.StockAsync(variante.ProductoId));

        var anularComoAdmin = await e.AnularVentaAsync(venta.Id);
        Assert.Equal(HttpStatusCode.OK, anularComoAdmin.StatusCode);
    }

    [Fact]
    public async Task Un_usuario_desactivado_no_puede_iniciar_sesion()
    {
        var admin = await _api.AdminAsync();
        var email = $"baja-{Guid.NewGuid():N}@gonzai.local";
        var crear = await admin.PostAsJsonAsync("/api/usuario", new UsuarioCreateDto
        {
            Nombre = "Baja", Email = email, Password = "Baja-12345", Rol = "Vendedor"
        });
        var creado = (await crear.Content.ReadFromJsonAsync<UsuarioResponseDto>())!;
        await admin.DeleteAsync($"/api/usuario/{creado.Id}");

        var r = await _api.Anonimo().PostAsJsonAsync("/api/usuario/login",
            new LoginDto { Email = email, Password = "Baja-12345" });

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task El_dashboard_refleja_una_venta_nueva_sin_esperar_a_que_venza_la_cache()
    {
        var e = new Escenario(await _api.AdminAsync());
        var antes = await TotalDeudaAsync(e.Admin);   // deja la caché poblada
        var cliente = await e.CrearClienteAsync();
        var variante = await e.CrearVarianteAsync(5);

        await e.VenderOkAsync(cliente.Id, variante.Id, 2);   // +20.000 de deuda

        Assert.Equal(antes + 20_000m, await TotalDeudaAsync(e.Admin));
    }

    [Fact]
    public async Task El_login_se_limita_a_10_intentos_por_minuto()
    {
        using var conLimite = _api.Factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:Enabled", "true"));
        var client = conLimite.CreateClient();
        var intento = new LoginDto { Email = "nadie@gonzai.local", Password = "mala-contraseña" };

        var codigos = new List<HttpStatusCode>();
        for (var i = 0; i < 12; i++)
            codigos.Add((await client.PostAsJsonAsync("/api/usuario/login", intento)).StatusCode);

        Assert.Equal(10, codigos.Count(c => c == HttpStatusCode.Unauthorized));
        Assert.Equal(2, codigos.Count(c => c == HttpStatusCode.TooManyRequests));
    }

    private static async Task<decimal> TotalDeudaAsync(HttpClient admin)
    {
        var json = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/cliente/total-deuda");
        return json.GetProperty("totalDeuda").GetDecimal();
    }
}
