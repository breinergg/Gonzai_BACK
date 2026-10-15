using Gonzai_API.DTOs.VentaCliente;

namespace Gonzai_API.Services.Interfaces;

public interface IVentaClienteService
{
    Task<IEnumerable<VentaClienteResponseDto>> GetAllAsync(
        int? clienteId = null, DateOnly? desde = null, DateOnly? hasta = null, bool soloPendientes = false, int? limit = null);
    Task<VentaClienteResponseDto?> GetByIdAsync(int id);
    Task<VentaClienteResponseDto> CreateAsync(VentaClienteCreateDto dto, int usuarioId);
    Task<VentaClienteResponseDto?> AnularAsync(int id, string motivo, int usuarioId);
    Task<VentaMensualResumenDto> GetResumenMensualAsync();
}
