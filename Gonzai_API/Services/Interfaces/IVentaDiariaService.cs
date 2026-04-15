using Gonzai_API.DTOs.VentaDiaria;

namespace Gonzai_API.Services.Interfaces;

public interface IVentaDiariaService
{
    Task<IEnumerable<VentaDiariaResponseDto>> GetAllAsync();
    Task<IEnumerable<VentaDiariaResponseDto>> GetByFechaAsync(DateOnly fecha);
    Task<IEnumerable<VentaDiariaResponseDto>> GetByRangoFechaAsync(DateOnly desde, DateOnly hasta);
    Task<VentaDiariaResponseDto?> GetByIdAsync(int id);
    Task<VentaDiariaResponseDto> CreateAsync(VentaDiariaCreateDto dto);
    Task<VentaDiariaResponseDto?> UpdateAsync(int id, VentaDiariaUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}
