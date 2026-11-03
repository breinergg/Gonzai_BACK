using Gonzai_API.DTOs.Comun;
using Gonzai_API.DTOs.VentaCliente;

namespace Gonzai_API.Services.Interfaces;

public interface IVentaClienteService
{
    Task<PagedResultDto<VentaClienteResponseDto>> GetAllAsync(
        int? clienteId = null, DateOnly? desde = null, DateOnly? hasta = null,
        bool soloPendientes = false, int page = 1, int pageSize = 50);
    Task<VentaClienteResponseDto?> GetByIdAsync(int id);
    Task<VentaClienteResponseDto> CreateAsync(VentaClienteCreateDto dto, int usuarioId);
    Task<VentaClienteResponseDto?> AnularAsync(int id, string motivo, int usuarioId);
    Task<VentaMensualResumenDto> GetResumenMensualAsync();
}
