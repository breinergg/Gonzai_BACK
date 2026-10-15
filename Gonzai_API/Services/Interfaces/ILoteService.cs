using Gonzai_API.DTOs.Lote;

namespace Gonzai_API.Services.Interfaces;

public interface ILoteService
{
    /// <summary>Lotes (compras) con su resumen de unidades y rentabilidad, del más reciente al más antiguo.</summary>
    Task<IEnumerable<LoteResumenDto>> GetAllAsync(int? proveedorId = null, bool soloConStock = false);

    /// <summary>Lote con su rentabilidad y el avance de cada prenda.</summary>
    Task<LoteDetalleDto?> GetByIdAsync(int compraProveedorId);

    Task<LoteDetalleDto?> GetByCodigoAsync(string codigo);

    /// <summary>Stock disponible por lote, en orden FIFO (el primero es el que se vende por defecto).</summary>
    Task<IEnumerable<ExistenciaDisponibleDto>> GetDisponiblesAsync(int? productoId, int? productoVarianteId);
}
