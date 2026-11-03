using Gonzai_API.Data;
using Gonzai_API.Models.Plataforma;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Gonzai_API.Services.Tenancy;

public class TenantDbFactory : ITenantDbFactory
{
    private readonly ITenantStore _store;
    private readonly IMemoryCache _cache;

    public TenantDbFactory(ITenantStore store, IMemoryCache cache)
    {
        _store = store;
        _cache = cache;
    }

    public AppDbContext Crear(Empresa empresa) =>
        CrearPorCadena(_store.CadenaConexion(empresa), empresa.Id);

    public AppDbContext CrearPorCadena(string cadenaConexion, int empresaId)
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cadenaConexion)
            .AddInterceptors(new DashboardCacheInterceptor(_cache, empresaId))
            .Options;

        return new AppDbContext(opciones);
    }
}
