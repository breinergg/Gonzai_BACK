using Gonzai_API.Data;
using Gonzai_API.Models.Plataforma;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

namespace Gonzai_API.Services.Tenancy;

public class TenantStore : ITenantStore
{
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(5);

    private readonly PlataformaDbContext _catalogo;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;

    public TenantStore(PlataformaDbContext catalogo, IMemoryCache cache, IConfiguration config)
    {
        _catalogo = catalogo;
        _cache    = cache;
        _config   = config;
    }

    public async Task<Empresa?> PorIdAsync(int empresaId, CancellationToken ct = default) =>
        await _cache.GetOrCreateAsync($"empresa:{empresaId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Vigencia;
            return await _catalogo.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == empresaId, ct);
        });

    public async Task<Empresa?> PorEmailDeUsuarioAsync(string email, CancellationToken ct = default)
    {
        var normalizado = email.Trim().ToLowerInvariant();

        // El login no se cachea: debe ver de inmediato una empresa dada de baja o un usuario nuevo.
        return await _catalogo.UsuariosDirectorio
            .AsNoTracking()
            .Where(u => u.Email == normalizado)
            .Select(u => u.Empresa)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Empresa>> ActivasAsync(CancellationToken ct = default) =>
        await _catalogo.Empresas.AsNoTracking().Where(e => e.Activa).OrderBy(e => e.Id).ToListAsync(ct);

    public string CadenaConexion(Empresa empresa) => Construir(_config, empresa.BaseDatos);

    public string CadenaMantenimiento() =>
        Construir(_config, _config["Tenancy:MaintenanceDatabase"] ?? "postgres");

    public string NombreBaseDatos(string slug) =>
        $"{_config["Tenancy:DatabasePrefix"] ?? "gonzai_emp_"}{slug}";

    public void OlvidarCache(int empresaId) => _cache.Remove($"empresa:{empresaId}");

    /// <summary>
    /// Base donde vive el catálogo de empresas. Es distinta de la base de cada empresa, y su
    /// nombre se configura aparte para que la cadena de conexión siga siendo una sola.
    /// </summary>
    public static string CadenaCatalogo(IConfiguration config) =>
        Construir(config, config["Tenancy:CatalogDatabase"] ?? "gonzai_catalogo");

    /// <summary>
    /// Toma la conexión al servidor y le cambia el nombre de la base de datos. Así la contraseña
    /// de PostgreSQL vive en un solo sitio y no hay que guardarla una vez por empresa.
    /// </summary>
    private static string Construir(IConfiguration config, string baseDatos)
    {
        var plantilla = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión 'DefaultConnection' para llegar a PostgreSQL.");

        var builder = new NpgsqlConnectionStringBuilder(plantilla) { Database = baseDatos };

        // Cada empresa abre su propio pool: con muchas empresas, un pool grande agotaría
        // las conexiones de PostgreSQL (max_connections, 100 por defecto).
        builder.MaxPoolSize = config.GetValue("Tenancy:MaxPoolSize", 10);

        return builder.ConnectionString;
    }
}
