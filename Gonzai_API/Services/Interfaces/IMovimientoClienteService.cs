using Gonzai_API.DTOs.MovimientoCliente;

namespace Gonzai_API.Services.Interfaces;

public interface IMovimientoClienteService
{
    Task<IEnumerable<MovimientoClienteResponseDto>> GetAllAsync();
    Task<IEnumerable<MovimientoClienteResponseDto>> GetByClienteIdAsync(int clienteId);
    Task<MovimientoClienteResponseDto?> GetByIdAsync(int id);
    Task<MovimientoClienteResponseDto> CreateAsync(MovimientoClienteCreateDto dto);
}
