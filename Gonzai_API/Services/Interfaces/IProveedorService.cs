using Gonzai_API.DTOs.Proveedor;

namespace Gonzai_API.Services.Interfaces;

public interface IProveedorService
{
    Task<IEnumerable<ProveedorResponseDto>> GetAllAsync(bool soloActivos = false);
    Task<ProveedorResponseDto?> GetByIdAsync(int id);
    Task<ProveedorResponseDto> CreateAsync(ProveedorCreateDto dto);
    Task<ProveedorResponseDto?> UpdateAsync(int id, ProveedorUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}
