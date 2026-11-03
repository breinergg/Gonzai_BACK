using Gonzai_API.DTOs.Usuario;

namespace Gonzai_API.Services.Interfaces;

/// <summary>
/// Inicio de sesión. Es lo único que ocurre antes de saber a qué empresa pertenece quien entra,
/// por eso vive aparte de <see cref="IUsuarioService"/>, que siempre trabaja dentro de una empresa.
/// </summary>
public interface IAutenticacionService
{
    Task<TokenResponseDto?> LoginAsync(LoginDto dto, CancellationToken ct = default);
}
