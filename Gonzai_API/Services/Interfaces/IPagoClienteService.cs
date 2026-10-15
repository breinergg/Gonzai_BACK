using Gonzai_API.DTOs.PagoCliente;

namespace Gonzai_API.Services.Interfaces;

public interface IPagoClienteService
{
    Task<IEnumerable<PagoClienteResponseDto>> GetByVentaIdAsync(int ventaClienteId);
    Task<IEnumerable<PagoClienteResponseDto>> GetByClienteIdAsync(int clienteId);
    Task<PagoClienteResponseDto?> GetByIdAsync(int id);
    Task<PagoClienteResponseDto> CreateAsync(PagoClienteCreateDto dto, int usuarioId);
    Task<PagoClienteResponseDto?> AnularAsync(int id, string motivo, int usuarioId);
}
