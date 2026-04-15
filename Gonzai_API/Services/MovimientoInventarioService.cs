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

    public MovimientoInventarioService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<MovimientoInventarioResponseDto>> GetAllAsync()
    {
        var movimientos = await _context.MovimientosInventario
            .AsNoTracking()
            .Include(m => m.Producto)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();

        return _mapper.Map<IEnumerable<MovimientoInventarioResponseDto>>(movimientos);
    }

    public async Task<IEnumerable<MovimientoInventarioResponseDto>> GetByProductoIdAsync(int productoId)
    {
        var movimientos = await _context.MovimientosInventario
            .AsNoTracking()
            .Include(m => m.Producto)
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();

        return _mapper.Map<IEnumerable<MovimientoInventarioResponseDto>>(movimientos);
    }

    public async Task<MovimientoInventarioResponseDto?> GetByIdAsync(int id)
    {
        var movimiento = await _context.MovimientosInventario
            .AsNoTracking()
            .Include(m => m.Producto)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movimiento is null) return null;

        return _mapper.Map<MovimientoInventarioResponseDto>(movimiento);
    }

    public async Task<MovimientoInventarioResponseDto> CreateAsync(MovimientoInventarioCreateDto dto)
    {
        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.Id == dto.ProductoId && p.Activo);

        if (producto is null)
            throw new KeyNotFoundException($"No se encontró un producto activo con Id {dto.ProductoId}.");

        var movimiento = _mapper.Map<MovimientoInventario>(dto);

        // Actualizar stock según tipo de movimiento
        if (dto.TipoMovimiento.Equals("entrada", StringComparison.OrdinalIgnoreCase))
        {
            producto.StockActual += dto.Cantidad;
        }
        else if (dto.TipoMovimiento.Equals("salida", StringComparison.OrdinalIgnoreCase))
        {
            if (producto.StockActual < dto.Cantidad)
                throw new InvalidOperationException(
                    $"Stock insuficiente. Stock actual: {producto.StockActual}, cantidad solicitada: {dto.Cantidad}.");

            producto.StockActual -= dto.Cantidad;
        }

        _context.MovimientosInventario.Add(movimiento);
        await _context.SaveChangesAsync();

        // Recargar con el nombre del producto para el response
        await _context.Entry(movimiento)
            .Reference(m => m.Producto)
            .LoadAsync();

        return _mapper.Map<MovimientoInventarioResponseDto>(movimiento);
    }
}
