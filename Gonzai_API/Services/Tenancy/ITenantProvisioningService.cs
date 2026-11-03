using Gonzai_API.DTOs.Plataforma;

namespace Gonzai_API.Services.Tenancy;

public interface ITenantProvisioningService
{
    /// <summary>
    /// Da de alta una empresa: crea su base de datos, aplica las migraciones y le deja
    /// su propio administrador. Cada empresa queda completamente separada de las demás.
    /// </summary>
    Task<EmpresaResponseDto> CrearAsync(EmpresaCreateDto dto, CancellationToken ct = default);

    /// <summary>
    /// Registra como empresa una base de datos que ya existe, sin copiar nada: aplica las
    /// migraciones pendientes e inscribe en el directorio los correos de sus usuarios actuales.
    /// Sirve para la primera empresa (la base que ya venía funcionando) y para restaurar respaldos.
    /// </summary>
    Task<EmpresaResponseDto> AdoptarAsync(string nombre, string slug, string baseDatos, CancellationToken ct = default);

    Task<IReadOnlyList<EmpresaResponseDto>> ListarAsync(CancellationToken ct = default);

    Task<EmpresaResponseDto?> CambiarEstadoAsync(int empresaId, bool activa, CancellationToken ct = default);

    /// <summary>Aplica las migraciones pendientes en la base de cada empresa activa.</summary>
    Task<int> MigrarTodasAsync(CancellationToken ct = default);
}
