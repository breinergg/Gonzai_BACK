using System.Net.Http.Headers;
using System.Net.Http.Json;
using Gonzai_API.DTOs.Plataforma;
using Gonzai_API.DTOs.Usuario;
using Gonzai_API.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Gonzai_API.Tests;

[CollectionDefinition(Nombre)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Nombre = "api";
}

/// <summary>
/// Levanta un PostgreSQL real en Docker (el código usa SELECT ... FOR UPDATE, que SQLite/InMemory
/// no soportan), arranca la API completa en memoria y da de alta dos empresas distintas para
/// poder comprobar que ninguna ve los datos de la otra.
/// </summary>
public class ApiFixture : IAsyncLifetime
{
    public const string ApiKeyPlataforma = "clave-de-plataforma-para-pruebas";
    public const string Password         = "Admin-Test-123";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = default!;

    /// <summary>Empresa principal. La usan las pruebas que no hablan de varias empresas.</summary>
    public EmpresaDePrueba EmpresaA { get; private set; } = default!;

    /// <summary>Segunda empresa, para comprobar el aislamiento.</summary>
    public EmpresaDePrueba EmpresaB { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", _db.GetConnectionString());
            builder.UseSetting("Jwt:Key", "clave-de-pruebas-con-mas-de-32-caracteres-!!");
            builder.UseSetting("Gemini:ApiKey", "no-se-usa-en-pruebas");
            builder.UseSetting("Database:MigrateOnStartup", "true");
            builder.UseSetting("Plataforma:ApiKey", ApiKeyPlataforma);
            builder.UseSetting("RateLimiting:Enabled", "false");
            // La primera empresa se crea sola al arrancar, como en producción.
            builder.UseSetting("Seed:Empresa:Nombre", "Empresa A");
            builder.UseSetting("Seed:Empresa:Slug", "empresa_a");
            builder.UseSetting("Seed:AdminEmail", "admin.a@gonzai.local");
            builder.UseSetting("Seed:AdminPassword", Password);
        });

        EmpresaA = new EmpresaDePrueba("Empresa A", "admin.a@gonzai.local");
        EmpresaB = await DarDeAltaAsync("Empresa B", "empresa_b", "admin.b@gonzai.local");
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _db.DisposeAsync();
    }

    public HttpClient Anonimo() => Factory.CreateClient();

    public HttpClient Plataforma()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyPlataformaAttribute.Header, ApiKeyPlataforma);
        return client;
    }

    /// <summary>Cliente autenticado como administrador de la empresa principal.</summary>
    public Task<HttpClient> AdminAsync() => ConTokenAsync(EmpresaA.AdminEmail, Password);

    public async Task<HttpClient> ConTokenAsync(string email, string password)
    {
        var client = Factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/usuario/login", new LoginDto { Email = email, Password = password });
        resp.EnsureSuccessStatusCode();
        var token = (await resp.Content.ReadFromJsonAsync<TokenResponseDto>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<EmpresaDePrueba> DarDeAltaAsync(
        string nombre, string slug, string adminEmail, int limiteChatsDiarios = 200)
    {
        var resp = await Plataforma().PostAsJsonAsync("/api/plataforma/empresas", new EmpresaCreateDto
        {
            Nombre             = nombre,
            Slug               = slug,
            AdminNombre        = $"Admin {nombre}",
            AdminEmail         = adminEmail,
            AdminPassword      = Password,
            LimiteChatsDiarios = limiteChatsDiarios
        });
        resp.EnsureSuccessStatusCode();

        var empresa = (await resp.Content.ReadFromJsonAsync<EmpresaResponseDto>())!;
        return new EmpresaDePrueba(nombre, adminEmail, empresa.Id);
    }

    /// <summary>Conexión directa a la base de una empresa, para montar casos que la API no permite crear.</summary>
    public string CadenaDeEmpresa(string slug)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(_db.GetConnectionString())
        {
            Database = $"gonzai_emp_{slug}"
        };
        return builder.ConnectionString;
    }

    public record EmpresaDePrueba(string Nombre, string AdminEmail, int? Id = null);
}
