using Gonzai_API.Data;
using Gonzai_API.DTOs.Proveedor;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class ProveedorService : IProveedorService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public ProveedorService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProveedorResponseDto>> GetAllAsync(bool soloActivos = false)
    {
        var proveedores = await _context.Proveedores
            .AsNoTracking()
            .Where(p => !soloActivos || p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProveedorResponseDto>>(proveedores);
    }

    public async Task<ProveedorResponseDto?> GetByIdAsync(int id)
    {
        var proveedor = await _context.Proveedores
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        return proveedor is null ? null : _mapper.Map<ProveedorResponseDto>(proveedor);
    }

    public async Task<ProveedorResponseDto> CreateAsync(ProveedorCreateDto dto)
    {
        var proveedor = _mapper.Map<Proveedor>(dto);

        _context.Proveedores.Add(proveedor);
        await _context.SaveChangesAsync();

        return _mapper.Map<ProveedorResponseDto>(proveedor);
    }

    public async Task<ProveedorResponseDto?> UpdateAsync(int id, ProveedorUpdateDto dto)
    {
        var proveedor = await _context.Proveedores.FindAsync(id);

        if (proveedor is null) return null;

        _mapper.Map(dto, proveedor);
        await _context.SaveChangesAsync();

        return _mapper.Map<ProveedorResponseDto>(proveedor);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var proveedor = await _context.Proveedores.FindAsync(id);

        if (proveedor is null) return false;

        // Soft delete: preserva el historial de compras del proveedor
        proveedor.Activo = false;
        await _context.SaveChangesAsync();

        return true;
    }
}
