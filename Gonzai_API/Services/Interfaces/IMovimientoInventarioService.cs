using Gonzai_API.DTOs.MovimientoInventario;

namespace Gonzai_API.Services.Interfaces;

public interface IMovimientoInventarioService
{
    Task<IEnumerable<MovimientoInventarioResponseDto>> GetAllAsync();
    Task<IEnumerable<MovimientoInventarioResponseDto>> GetByProductoIdAsync(int productoId);
    Task<IEnumerable<MovimientoInventarioResponseDto>> GetRecentAsync(int? productoId, string? tipo, int limit = 50);
    Task<MovimientoInventarioResponseDto?> GetByIdAsync(int id);
    Task<MovimientoInventarioResponseDto> CreateAsync(MovimientoInventarioCreateDto dto);
}
