using Gonzai_API.Data;
using Gonzai_API.Models.Plataforma;

namespace Gonzai_API.Services.Tenancy;

/// <summary>
/// Abre la base de datos de una empresa concreta. Se usa donde la petición todavía no tiene
/// empresa asignada: el login, los comprobantes públicos y el alta de empresas nuevas.
/// </summary>
public interface ITenantDbFactory
{
    AppDbContext Crear(Empresa empresa);

    AppDbContext CrearPorCadena(string cadenaConexion, int empresaId);
}
