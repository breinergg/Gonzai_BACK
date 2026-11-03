using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gonzai_API.Data;

/// <summary>
/// Solo para las herramientas de línea de comandos (dotnet ef). En ejecución, la conexión de
/// <see cref="AppDbContext"/> sale de la empresa del token, que en diseño no existe.
/// La cadena se toma de GONZAI_DESIGN_CONNECTION si está definida.
/// </summary>
public class AppDbContextDesignFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(Cadena()).Options);

    internal static string Cadena() =>
        Environment.GetEnvironmentVariable("GONZAI_DESIGN_CONNECTION")
        ?? "Host=localhost;Database=gonzai_design;Username=postgres;Password=postgres";
}

public class PlataformaDbContextDesignFactory : IDesignTimeDbContextFactory<PlataformaDbContext>
{
    public PlataformaDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PlataformaDbContext>()
            .UseNpgsql(AppDbContextDesignFactory.Cadena()).Options);
}
