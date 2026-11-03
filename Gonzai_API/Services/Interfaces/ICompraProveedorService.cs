using Gonzai_API.DTOs.Comun;
using Gonzai_API.DTOs.CompraProveedor;

namespace Gonzai_API.Services.Interfaces;

public interface ICompraProveedorService
{
    Task<PagedResultDto<CompraProveedorResponseDto>> GetAllAsync(
        int? proveedorId = null, DateOnly? desde = null, DateOnly? hasta = null,
        int page = 1, int pageSize = 50);
    Task<CompraProveedorResponseDto?> GetByIdAsync(int id);
    Task<CompraProveedorResponseDto> CreateAsync(CompraProveedorCreateDto dto, int usuarioId);
    Task<CompraProveedorResponseDto?> AnularAsync(int id, string motivo, int usuarioId);
}
