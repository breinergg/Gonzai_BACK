using Gonzai_API.Models.Plataforma;

namespace Gonzai_API.Services.Tenancy;

/// <summary>Busca empresas en el catálogo y arma su cadena de conexión.</summary>
public interface ITenantStore
{
    Task<Empresa?> PorIdAsync(int empresaId, CancellationToken ct = default);

    Task<Empresa?> PorEmailDeUsuarioAsync(string email, CancellationToken ct = default);

    Task<IReadOnlyList<Empresa>> ActivasAsync(CancellationToken ct = default);

    /// <summary>Cadena de conexión a la base de datos de la empresa.</summary>
    string CadenaConexion(Empresa empresa);

    /// <summary>Cadena de conexión a la base de mantenimiento, para crear bases nuevas.</summary>
    string CadenaMantenimiento();

    string NombreBaseDatos(string slug);

    void OlvidarCache(int empresaId);
}
