using Gonzai_API.DTOs.Producto;

namespace Gonzai_API.Services.Interfaces;

public interface IProductoService
{
    Task<IEnumerable<ProductoResponseDto>> GetAllAsync();
    Task<IEnumerable<ProductoResponseDto>> GetAllActivosAsync();
    Task<ProductoResponseDto?> GetByIdAsync(int id);
    Task<ProductoResponseDto> CreateAsync(ProductoCreateDto dto, int usuarioId);
    Task<ProductoResponseDto?> UpdateAsync(int id, ProductoUpdateDto dto);
    Task<bool> DeleteAsync(int id);
    Task<int> GetProductosActivosCountAsync();
    Task<IEnumerable<ProductoResponseDto>> SearchAsync(string? nombre, bool soloActivos = true, int limit = 20);

    Task<ProductoVarianteResponseDto?> GetVarianteByIdAsync(int varianteId);
    Task<ProductoVarianteResponseDto> CreateVarianteAsync(int productoId, ProductoVarianteCreateDto dto, int usuarioId);
    /// <summary>Crea las combinaciones talla × color que falten; las existentes se omiten.</summary>
    Task<IEnumerable<ProductoVarianteResponseDto>> CreateMatrizAsync(int productoId, ProductoVarianteMatrizDto dto, int usuarioId);
    Task<ProductoVarianteResponseDto?> UpdateVarianteAsync(int varianteId, ProductoVarianteUpdateDto dto);
    Task<bool> DeleteVarianteAsync(int varianteId);
}
