using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gonzai_API.DTOs.Comprobante;
using Gonzai_API.DTOs.Plataforma;
using Gonzai_API.DTOs.Usuario;

namespace Gonzai_API.Tests;

/// <summary>
/// Lo que ninguna empresa debe poder hacer: ver, tocar o deducir los datos de otra.
/// </summary>
[Collection(ApiCollection.Nombre)]
public class MultiEmpresaTests
{
    private readonly ApiFixture _api;
    public MultiEmpresaTests(ApiFixture api) => _api = api;

    private async Task<(Escenario a, Escenario b)> DosEmpresasAsync() =>
        (new Escenario(await _api.AdminAsync()),
         new Escenario(await _api.ConTokenAsync(_api.EmpresaB.AdminEmail, ApiFixture.Password)));

    [Fact]
    public async Task Los_clientes_de_una_empresa_no_aparecen_en_la_otra()
    {
        var (a, b) = await DosEmpresasAsync();
        var cliente = await a.CrearClienteAsync();

        var listaDeB = await b.Admin.GetFromJsonAsync<JsonElement>("/api/cliente");

        Assert.DoesNotContain(cliente.Nombre, listaDeB.ToString());
    }

    [Fact]
    public async Task Una_empresa_no_puede_abrir_la_venta_de_otra_por_su_Id()
    {
        var (a, b) = await DosEmpresasAsync();
        var cliente = await a.CrearClienteAsync();
        var variante = await a.CrearVarianteAsync(5);
        var venta = await a.VenderOkAsync(cliente.Id, variante.Id, 2);

        var respuesta = await b.Admin.GetAsync($"/api/ventacliente/{venta.Id}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task La_deuda_del_dashboard_no_mezcla_las_dos_empresas()
    {
        var (a, b) = await DosEmpresasAsync();

        var deudaInicialB = await TotalDeudaAsync(b);

        var cliente = await a.CrearClienteAsync();
        var variante = await a.CrearVarianteAsync(5);
        await a.VenderOkAsync(cliente.Id, variante.Id, 3);   // +30.000 de deuda, solo en A

        // Se lee A primero para dejar SU cifra en la caché. Si la caché no separara por empresa,
        // la siguiente lectura de B devolvería el total de A.
        var deudaA = await TotalDeudaAsync(a);
        Assert.NotEqual(deudaInicialB, deudaA);

        Assert.Equal(deudaInicialB, await TotalDeudaAsync(b));
    }

    [Fact]
    public async Task El_stock_es_independiente_aunque_el_producto_se_llame_igual()
    {
        var (a, b) = await DosEmpresasAsync();
        var enA = await a.CrearVarianteAsync(stock: 10);
        var enB = await b.CrearVarianteAsync(stock: 4);

        var cliente = await a.CrearClienteAsync();
        await a.VenderOkAsync(cliente.Id, enA.Id, 6);

        Assert.Equal(4, await a.StockAsync(enA.ProductoId));
        Assert.Equal(4, await b.StockAsync(enB.ProductoId));   // B no se entera de la venta de A
    }

    [Fact]
    public async Task Cada_empresa_ve_su_propio_nombre_comercial()
    {
        var (a, b) = await DosEmpresasAsync();

        var negocioA = await a.Admin.GetFromJsonAsync<JsonElement>("/api/negocio");
        var negocioB = await b.Admin.GetFromJsonAsync<JsonElement>("/api/negocio");

        Assert.Equal("Empresa A", negocioA.GetProperty("nombreComercial").GetString());
        Assert.Equal("Empresa B", negocioB.GetProperty("nombreComercial").GetString());
    }

    [Fact]
    public async Task Un_correo_ya_registrado_en_una_empresa_no_se_puede_usar_en_otra()
    {
        var (_, b) = await DosEmpresasAsync();

        var respuesta = await b.Admin.PostAsJsonAsync("/api/usuario", new UsuarioCreateDto
        {
            Nombre = "Intruso", Email = _api.EmpresaA.AdminEmail, Password = "Otra-Clave-123", Rol = "Vendedor"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_comprobante_publico_solo_funciona_con_el_token_de_su_empresa()
    {
        var (a, _) = await DosEmpresasAsync();
        var cliente = await a.CrearClienteAsync();
        var variante = await a.CrearVarianteAsync(5);
        var venta = await a.VenderOkAsync(cliente.Id, variante.Id, 1);

        var compartir = await a.Admin.GetFromJsonAsync<CompartirComprobanteDto>(
            $"/api/ventacliente/{venta.Id}/comprobante/compartir");
        var token = compartir!.Url[(compartir.Url.LastIndexOf('/') + 1)..];

        var propio = await _api.Anonimo().GetAsync($"/api/comprobantes/{token}");
        Assert.Equal(HttpStatusCode.OK, propio.StatusCode);
        Assert.Equal("application/pdf", propio.Content.Headers.ContentType?.MediaType);

        // Cambiar la empresa del token no da acceso: el token completo no existe en la otra base.
        var empresaB = _api.EmpresaB.Id!.Value;
        var suplantado = $"{empresaB}{token[token.IndexOf('-')..]}";
        var ajeno = await _api.Anonimo().GetAsync($"/api/comprobantes/{suplantado}");

        Assert.Equal(HttpStatusCode.NotFound, ajeno.StatusCode);
    }

    [Fact]
    public async Task Una_empresa_desactivada_no_puede_entrar_ni_seguir_usando_su_token()
    {
        var empresa = await _api.DarDeAltaAsync("Empresa Morosa", "morosa", "admin.morosa@gonzai.local");
        var cliente = await _api.ConTokenAsync(empresa.AdminEmail, ApiFixture.Password);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/cliente")).StatusCode);

        var baja = await _api.Plataforma().PatchAsJsonAsync(
            $"/api/plataforma/empresas/{empresa.Id}/estado", new { activa = false });
        baja.EnsureSuccessStatusCode();

        // El token ya emitido deja de servir...
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/cliente")).StatusCode);

        // ...y tampoco puede volver a iniciar sesión.
        var login = await _api.Anonimo().PostAsJsonAsync("/api/usuario/login",
            new LoginDto { Email = empresa.AdminEmail, Password = ApiFixture.Password });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, login.StatusCode);
    }

    [Fact]
    public async Task El_alta_de_empresas_exige_la_clave_de_plataforma()
    {
        var sinClave = await _api.Anonimo().PostAsJsonAsync("/api/plataforma/empresas", new EmpresaCreateDto
        {
            Nombre = "Pirata", Slug = "pirata",
            AdminNombre = "x", AdminEmail = "pirata@gonzai.local", AdminPassword = "Clave-12345"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, sinClave.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.Anonimo().GetAsync("/api/plataforma/empresas")).StatusCode);
    }

    [Fact]
    public async Task Cada_empresa_tiene_su_propia_base_de_datos()
    {
        var empresas = await _api.Plataforma().GetFromJsonAsync<List<EmpresaResponseDto>>("/api/plataforma/empresas");

        var bases = empresas!.Select(e => e.BaseDatos).ToList();
        Assert.Equal(bases.Count, bases.Distinct().Count());
        Assert.All(bases, b => Assert.StartsWith("gonzai_emp_", b));
    }

    [Fact]
    public async Task Se_puede_entrar_aunque_el_correo_guardado_tenga_mayusculas()
    {
        // Caso real al adoptar una base de un respaldo: los correos pueden venir con mayúsculas.
        var empresa = await _api.DarDeAltaAsync("Empresa Heredada", "heredada", "jefe@heredada.local");

        await using (var conexion = new Npgsql.NpgsqlConnection(_api.CadenaDeEmpresa("heredada")))
        {
            await conexion.OpenAsync();
            await using var comando = new Npgsql.NpgsqlCommand(
                "UPDATE \"Usuario\" SET \"Email\" = 'Jefe@Heredada.Local' WHERE \"Email\" = 'jefe@heredada.local'",
                conexion);
            Assert.Equal(1, await comando.ExecuteNonQueryAsync());
        }

        var login = await _api.Anonimo().PostAsJsonAsync("/api/usuario/login",
            new LoginDto { Email = empresa.AdminEmail, Password = ApiFixture.Password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Una_empresa_sin_cupo_de_IA_no_llega_a_consumir_Gemini()
    {
        // El cupo se comprueba antes de llamar a Gemini: si no fuese así, esta prueba fallaría
        // al intentar usar la clave falsa configurada en el entorno de pruebas.
        var empresa = await _api.DarDeAltaAsync("Sin IA", "sin_ia", "jefe@sinia.local", limiteChatsDiarios: 0);
        var cliente = await _api.ConTokenAsync(empresa.AdminEmail, ApiFixture.Password);

        var respuesta = await cliente.PostAsJsonAsync("/api/ai/chat", new { pregunta = "¿Cuánto me deben?" });

        Assert.Equal(HttpStatusCode.TooManyRequests, respuesta.StatusCode);
        Assert.Contains("no está habilitado", await respuesta.Content.ReadAsStringAsync());
    }

    private static async Task<decimal> TotalDeudaAsync(Escenario e)
    {
        var json = await e.Admin.GetFromJsonAsync<JsonElement>("/api/cliente/total-deuda");
        return json.GetProperty("totalDeuda").GetDecimal();
    }
}
