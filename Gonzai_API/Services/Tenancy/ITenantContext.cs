namespace Gonzai_API.Services.Tenancy;

/// <summary>
/// Empresa a la que pertenece la petición en curso. Se resuelve del token JWT.
/// </summary>
public interface ITenantContext
{
    /// <summary>Id de la empresa, o null si la petición no está autenticada.</summary>
    int? EmpresaId { get; }

    string Nombre { get; }

    /// <summary>
    /// Id de la empresa, o excepción si no hay ninguna. Se usa donde trabajar sin empresa
    /// sería un error: así nunca se consulta "la base equivocada" en silencio.
    /// </summary>
    int EmpresaIdRequerido { get; }

    /// <summary>Cadena de conexión a la base de datos de esta empresa.</summary>
    string CadenaConexionRequerida { get; }
}
