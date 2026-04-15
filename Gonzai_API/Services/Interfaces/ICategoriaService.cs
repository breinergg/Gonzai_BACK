using Gonzai_API.DTOs.Categoria;

namespace Gonzai_API.Services.Interfaces;

public interface ICategoriaService
{
    Task<IEnumerable<CategoriaResponseDto>> GetAllAsync();
    Task<CategoriaResponseDto?> GetByIdAsync(int id);
    Task<CategoriaResponseDto> CreateAsync(CategoriaCreateDto dto);
    Task<CategoriaResponseDto?> UpdateAsync(int id, CategoriaUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}
