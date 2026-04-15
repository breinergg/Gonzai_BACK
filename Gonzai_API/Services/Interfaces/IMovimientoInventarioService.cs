using Gonzai_API.DTOs.MovimientoInventario;

namespace Gonzai_API.Services.Interfaces;

public interface IMovimientoInventarioService
{
    Task<IEnumerable<MovimientoInventarioResponseDto>> GetAllAsync();
    Task<IEnumerable<MovimientoInventarioResponseDto>> GetByProductoIdAsync(int productoId);
    Task<MovimientoInventarioResponseDto?> GetByIdAsync(int id);
    Task<MovimientoInventarioResponseDto> CreateAsync(MovimientoInventarioCreateDto dto);
}
