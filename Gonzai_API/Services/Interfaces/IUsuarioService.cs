using Gonzai_API.DTOs.Usuario;

namespace Gonzai_API.Services.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioResponseDto>> GetAllAsync();
    Task<UsuarioResponseDto?> GetByIdAsync(int id);
    Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto dto);
    Task<UsuarioResponseDto?> UpdateAsync(int id, UsuarioUpdateDto dto);
    Task<bool> DeleteAsync(int id);
    Task<TokenResponseDto?> LoginAsync(LoginDto dto);
    Task<bool> CambiarPasswordAsync(int id, CambiarPasswordDto dto);
}
