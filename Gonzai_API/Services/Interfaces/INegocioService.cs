using Gonzai_API.DTOs.Negocio;

namespace Gonzai_API.Services.Interfaces;

public interface INegocioService
{
    Task<NegocioResponseDto> GetAsync();
    Task<NegocioResponseDto> UpdateAsync(NegocioUpdateDto dto);
    Task<NegocioResponseDto> UpdateLogoAsync(byte[] png);
    Task<NegocioResponseDto> QuitarLogoAsync();
    Task<byte[]?> GetLogoAsync();
}
