using System.Net.Http.Headers;
using System.Net.Http.Json;
using Gonzai_API.DTOs.Usuario;
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
/// Levanta un PostgreSQL real en Docker (el código usa SELECT ... FOR UPDATE, que SQLite/InMemory no soportan),
/// aplica las migraciones y arranca la API completa en memoria.
/// </summary>
public class ApiFixture : IAsyncLifetime
{
    public const string AdminEmail    = "admin.test@gonzai.local";
    public const string AdminPassword = "Admin-Test-123";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = default!;

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
            builder.UseSetting("Seed:AdminEmail", AdminEmail);
            builder.UseSetting("Seed:AdminPassword", AdminPassword);
            builder.UseSetting("RateLimiting:Enabled", "false");
        });
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _db.DisposeAsync();
    }

    public HttpClient Anonimo() => Factory.CreateClient();

    public Task<HttpClient> AdminAsync() => ConTokenAsync(AdminEmail, AdminPassword);

    public async Task<HttpClient> ConTokenAsync(string email, string password)
    {
        var client = Factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/usuario/login", new LoginDto { Email = email, Password = password });
        resp.EnsureSuccessStatusCode();
        var token = (await resp.Content.ReadFromJsonAsync<TokenResponseDto>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
