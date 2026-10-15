using Gonzai_API.DTOs.MovimientoInventario;

namespace Gonzai_API.Services.Interfaces;

public interface IMovimientoInventarioService
{
    Task<IEnumerable<MovimientoInventarioResponseDto>> GetAllAsync();
    Task<IEnumerable<MovimientoInventarioResponseDto>> GetByProductoIdAsync(int productoId);
    Task<IEnumerable<MovimientoInventarioResponseDto>> GetRecentAsync(
        int? productoId, string? tipo, int limit = 50, string? motivo = null, int? productoVarianteId = null);
    Task<MovimientoInventarioResponseDto?> GetByIdAsync(int id);

    /// <summary>Ajuste manual. Una salida FIFO puede generar varios movimientos (uno por existencia).</summary>
    Task<IEnumerable<MovimientoInventarioResponseDto>> CreateAjusteAsync(MovimientoInventarioCreateDto dto, int usuarioId);
}
