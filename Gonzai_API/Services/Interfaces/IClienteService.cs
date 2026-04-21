using Gonzai_API.DTOs.Cliente;

namespace Gonzai_API.Services.Interfaces;

public interface IClienteService
{
    Task<IEnumerable<ClienteResponseDto>> GetAllAsync();
    Task<IEnumerable<ClienteResponseDto>> GetAllActivosAsync();
    Task<ClienteResponseDto?> GetByIdAsync(int id);
    Task<ClienteResponseDto> CreateAsync(ClienteCreateDto dto);
    Task<ClienteResponseDto?> UpdateAsync(int id, ClienteUpdateDto dto);
    Task<bool> DeleteAsync(int id);
    Task<ClienteMayorDeudaDto?> GetClienteConMayorDeudaAsync();
    Task<decimal> GetTotalDeudaClientesActivosAsync();
    Task<int> GetClientesActivosConDeudaCountAsync();
    Task<int> GetClientesActivosCountAsync();
}
