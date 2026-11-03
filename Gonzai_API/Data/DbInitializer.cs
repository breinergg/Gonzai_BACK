using Gonzai_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Data;

public static class DbInitializer
{
    /// <summary>
    /// Aplica migraciones pendientes (si Database:MigrateOnStartup = true) y crea el primer
    /// administrador cuando no existe ningún usuario y hay Seed:AdminEmail / Seed:AdminPassword.
    /// </summary>
    public static async Task InicializarAsync(IServiceProvider servicios)
    {
        using var scope = servicios.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (config.GetValue("Database:MigrateOnStartup", false))
        {
            logger.LogInformation("Aplicando migraciones pendientes...");
            await db.Database.MigrateAsync();
        }

        var email    = config["Seed:AdminEmail"];
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        if (await db.Usuarios.AnyAsync()) return;

        if (password.Length < 8)
            throw new InvalidOperationException("Seed:AdminPassword debe tener al menos 8 caracteres.");

        db.Usuarios.Add(new Usuario
        {
            Nombre       = config["Seed:AdminName"] ?? "Administrador",
            Email        = email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Rol          = "Admin",
            Activo       = true
        });
        await db.SaveChangesAsync();
        logger.LogWarning("Se creó el administrador inicial {Email}. Cambia su contraseña y retira Seed:AdminPassword.", email);
    }
}
