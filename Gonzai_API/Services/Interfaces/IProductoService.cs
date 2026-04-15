using Gonzai_API.DTOs.Producto;

namespace Gonzai_API.Services.Interfaces;

public interface IProductoService
{
    Task<IEnumerable<ProductoResponseDto>> GetAllAsync();
    Task<IEnumerable<ProductoResponseDto>> GetAllActivosAsync();
    Task<ProductoResponseDto?> GetByIdAsync(int id);
    Task<ProductoResponseDto> CreateAsync(ProductoCreateDto dto);
    Task<ProductoResponseDto?> UpdateAsync(int id, ProductoUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}
