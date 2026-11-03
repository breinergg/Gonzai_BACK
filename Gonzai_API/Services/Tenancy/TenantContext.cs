using Gonzai_API.Models.Plataforma;

namespace Gonzai_API.Services.Tenancy;

public class TenantContext : ITenantContext
{
    private Empresa? _empresa;
    private string? _cadenaConexion;

    public int? EmpresaId => _empresa?.Id;

    public string Nombre => _empresa?.Nombre ?? "(sin empresa)";

    public int EmpresaIdRequerido => _empresa?.Id
        ?? throw new UnauthorizedAccessException("La petición no está asociada a ninguna empresa.");

    public string CadenaConexionRequerida => _cadenaConexion
        ?? throw new UnauthorizedAccessException(
            "La petición no está asociada a ninguna empresa, así que no hay base de datos a la que conectarse.");

    public void Establecer(Empresa empresa, string cadenaConexion)
    {
        _empresa        = empresa;
        _cadenaConexion = cadenaConexion;
    }
}
