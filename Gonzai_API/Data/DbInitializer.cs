using Gonzai_API.DTOs.Plataforma;
using Gonzai_API.Services.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Data;

public static class DbInitializer
{
    /// <summary>
    /// Prepara el catálogo de la plataforma, da de alta la primera empresa si todavía no hay
    /// ninguna, y aplica las migraciones pendientes en la base de cada empresa.
    /// </summary>
    public static async Task InicializarAsync(IServiceProvider servicios)
    {
        using var scope = servicios.CreateScope();
        var config      = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger      = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        var catalogo    = scope.ServiceProvider.GetRequiredService<PlataformaDbContext>();
        var altas       = scope.ServiceProvider.GetRequiredService<ITenantProvisioningService>();

        if (!config.GetValue("Database:MigrateOnStartup", false)) return;

        logger.LogInformation("Aplicando migraciones del catálogo de la plataforma...");
        await catalogo.Database.MigrateAsync();

        if (!await catalogo.Empresas.AnyAsync())
            await CrearPrimeraEmpresaAsync(config, logger, altas);

        var empresas = await altas.MigrarTodasAsync();
        logger.LogInformation("Migraciones al día en {Cantidad} empresa(s).", empresas);
    }

    private static async Task CrearPrimeraEmpresaAsync(
        IConfiguration config, ILogger logger, ITenantProvisioningService altas)
    {
        var nombre = config["Seed:Empresa:Nombre"];
        var slug   = config["Seed:Empresa:Slug"] ?? nombre;

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(slug)) return;

        // Base que ya existe (la que venías usando): se adopta tal cual, sin perder datos.
        var existente = config["Seed:Empresa:BaseDatosExistente"];
        if (!string.IsNullOrWhiteSpace(existente))
        {
            await altas.AdoptarAsync(nombre, slug, existente);
            logger.LogWarning(
                "La base '{BaseDatos}' quedó registrada como la empresa '{Nombre}'. " +
                "Sus usuarios actuales ya pueden iniciar sesión.", existente, nombre);
            return;
        }

        var email    = config["Seed:AdminEmail"];
        var password = config["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        await altas.CrearAsync(new EmpresaCreateDto
        {
            Nombre        = nombre,
            Slug          = slug,
            AdminNombre   = config["Seed:AdminName"] ?? "Administrador",
            AdminEmail    = email,
            AdminPassword = password
        });

        logger.LogWarning(
            "Se creó la empresa '{Nombre}' con el administrador {Email}. " +
            "Cambia su contraseña y retira Seed:AdminPassword.", nombre, email);
    }
}
