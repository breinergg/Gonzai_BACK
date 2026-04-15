using Gonzai_API.DTOs.PreguntaNoReconocida;

namespace Gonzai_API.Services.Interfaces;

public interface IPreguntaNoReconocidaService
{
    Task<IEnumerable<PreguntaNoReconocidaResponseDto>> GetAllAsync();
    Task<PreguntaNoReconocidaResponseDto> CreateAsync(PreguntaNoReconocidaCreateDto dto);
}
