using Gonzai_API.Data;
using Gonzai_API.DTOs.MovimientoInventario;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class MovimientoInventarioService : IMovimientoInventarioService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IInventarioService _inventario;

    public MovimientoInventarioService(AppDbContext context, IMapper mapper, IInventarioService inventario)
    {
        _context    = context;
        _mapper     = mapper;
        _inventario = inventario;
    }

    public async Task<IEnumerable<MovimientoInventarioResponseDto>> GetAllAsync()
    {
        var movimientos = await QueryConDetalle()
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return _mapper.Map<IEnumerable<MovimientoInventarioResponseDto>>(movimientos);
    }

    public async Task<IEnumerable<MovimientoInventarioResponseDto>> GetByProductoIdAsync(int productoId)
    {
        var movimientos = await QueryConDetalle()
            .Where(m => m.ProductoVariante.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return _mapper.Map<IEnumerable<MovimientoInventarioResponseDto>>(movimientos);
    }

    public async Task<IEnumerable<MovimientoInventarioResponseDto>> GetRecentAsync(
        int? productoId, string? tipo, int limit = 50, string? motivo = null, int? productoVarianteId = null)
    {
        var query = QueryConDetalle();

        if (productoId.HasValue)
            query = query.Where(m => m.ProductoVariante.ProductoId == productoId.Value);

        if (productoVarianteId.HasValue)
            query = query.Where(m => m.ProductoVarianteId == productoVarianteId.Value);

        if (!string.IsNullOrWhiteSpace(tipo))
            query = query.Where(m => m.TipoMovimiento == tipo.Trim().ToLower());

        if (!string.IsNullOrWhiteSpace(motivo))
            query = query.Where(m => m.Motivo == motivo.Trim());

        var movimientos = await query
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .Take(limit)
            .ToListAsync();

        return _mapper.Map<IEnumerable<MovimientoInventarioResponseDto>>(movimientos);
    }

    public async Task<MovimientoInventarioResponseDto?> GetByIdAsync(int id)
    {
        var movimiento = await QueryConDetalle().FirstOrDefaultAsync(m => m.Id == id);

        return movimiento is null ? null : _mapper.Map<MovimientoInventarioResponseDto>(movimiento);
    }

    public async Task<IEnumerable<MovimientoInventarioResponseDto>> CreateAjusteAsync(
        MovimientoInventarioCreateDto dto, int usuarioId)
    {
        if (dto.CompraProveedorId.HasValue && dto.ExistenciaId.HasValue)
            throw new InvalidOperationException("Indique el lote (compra) o la existencia, no ambos.");

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        var variante = (await _inventario.BloquearVariantesAsync([dto.ProductoVarianteId]))[dto.ProductoVarianteId];

        var ahora = DateTime.UtcNow;

        if (dto.TipoMovimiento == TiposMovimientoInventario.Entrada)
        {
            _inventario.RegistrarEntrada(new EntradaInventario(
                variante, dto.Cantidad, dto.CostoUnitario ?? variante.PrecioCompra ?? 0m,
                MotivosMovimientoInventario.AjusteManual, usuarioId, ahora, dto.Descripcion));
        }
        else
        {
            await _inventario.RegistrarSalidaAsync(new SalidaInventario(
                variante, dto.Cantidad, MotivosMovimientoInventario.AjusteManual, usuarioId, ahora, dto.Descripcion,
                CompraProveedorId: dto.CompraProveedorId, ExistenciaLoteId: dto.ExistenciaId));
        }

        var nuevos = _context.ChangeTracker.Entries<MovimientoInventario>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        var ids = nuevos.Select(m => m.Id).ToList();
        var movimientos = await QueryConDetalle()
            .Where(m => ids.Contains(m.Id))
            .OrderBy(m => m.Id)
            .ToListAsync();

        return _mapper.Map<IEnumerable<MovimientoInventarioResponseDto>>(movimientos);
    }

    private IQueryable<MovimientoInventario> QueryConDetalle() =>
        _context.MovimientosInventario
            .AsNoTracking()
            .Include(m => m.ProductoVariante).ThenInclude(v => v.Producto)
            .Include(m => m.ExistenciaLote).ThenInclude(e => e.CompraProveedorDetalle).ThenInclude(d => d!.CompraProveedor)
            .Include(m => m.Usuario);
}
