using Gonzai_API.DTOs.CompraProveedor;

namespace Gonzai_API.Services.Interfaces;

public interface ICompraProveedorService
{
    Task<IEnumerable<CompraProveedorResponseDto>> GetAllAsync(int? proveedorId = null, DateOnly? desde = null, DateOnly? hasta = null);
    Task<CompraProveedorResponseDto?> GetByIdAsync(int id);
    Task<CompraProveedorResponseDto> CreateAsync(CompraProveedorCreateDto dto, int usuarioId);
    Task<CompraProveedorResponseDto?> AnularAsync(int id, string motivo, int usuarioId);
}
