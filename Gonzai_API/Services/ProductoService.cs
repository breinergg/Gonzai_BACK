using Gonzai_API.Data;
using Gonzai_API.DTOs.Producto;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class ProductoService : IProductoService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public ProductoService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductoResponseDto>> GetAllAsync()
    {
        var productos = await _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProductoResponseDto>>(productos);
    }

    public async Task<IEnumerable<ProductoResponseDto>> GetAllActivosAsync()
    {
        var productos = await _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProductoResponseDto>>(productos);
    }

    public async Task<ProductoResponseDto?> GetByIdAsync(int id)
    {
        var producto = await _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (producto is null) return null;

        return _mapper.Map<ProductoResponseDto>(producto);
    }

    public async Task<ProductoResponseDto> CreateAsync(ProductoCreateDto dto)
    {
        if (dto.CategoriaId.HasValue)
        {
            var categoriaExiste = await _context.Categorias
                .AnyAsync(c => c.Id == dto.CategoriaId.Value);

            if (!categoriaExiste)
                throw new KeyNotFoundException($"No se encontró la categoría con Id {dto.CategoriaId}.");
        }

        var producto = _mapper.Map<Producto>(dto);

        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        // Recargar con la categoría para el response
        await _context.Entry(producto)
            .Reference(p => p.Categoria)
            .LoadAsync();

        return _mapper.Map<ProductoResponseDto>(producto);
    }

    public async Task<ProductoResponseDto?> UpdateAsync(int id, ProductoUpdateDto dto)
    {
        var producto = await _context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (producto is null) return null;

        if (dto.CategoriaId.HasValue)
        {
            var categoriaExiste = await _context.Categorias
                .AnyAsync(c => c.Id == dto.CategoriaId.Value);

            if (!categoriaExiste)
                throw new KeyNotFoundException($"No se encontró la categoría con Id {dto.CategoriaId}.");
        }

        _mapper.Map(dto, producto);
        await _context.SaveChangesAsync();

        return _mapper.Map<ProductoResponseDto>(producto);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var producto = await _context.Productos.FindAsync(id);

        if (producto is null) return false;

        // Soft delete: preserva historial de movimientos de inventario
        producto.Activo = false;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetProductosActivosCountAsync()
    {
        return await _context.Productos
            .AsNoTracking()
            .CountAsync(p => p.Activo);
    }
}
