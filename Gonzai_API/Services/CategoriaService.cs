using Gonzai_API.Data;
using Gonzai_API.DTOs.Categoria;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class CategoriaService : ICategoriaService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public CategoriaService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CategoriaResponseDto>> GetAllAsync()
    {
        var categorias = await _context.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        return _mapper.Map<IEnumerable<CategoriaResponseDto>>(categorias);
    }

    public async Task<CategoriaResponseDto?> GetByIdAsync(int id)
    {
        var categoria = await _context.Categorias
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (categoria is null) return null;

        return _mapper.Map<CategoriaResponseDto>(categoria);
    }

    public async Task<CategoriaResponseDto> CreateAsync(CategoriaCreateDto dto)
    {
        var categoria = _mapper.Map<Categoria>(dto);

        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();

        return _mapper.Map<CategoriaResponseDto>(categoria);
    }

    public async Task<CategoriaResponseDto?> UpdateAsync(int id, CategoriaUpdateDto dto)
    {
        var categoria = await _context.Categorias.FindAsync(id);

        if (categoria is null) return null;

        _mapper.Map(dto, categoria);
        await _context.SaveChangesAsync();

        return _mapper.Map<CategoriaResponseDto>(categoria);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var categoria = await _context.Categorias.FindAsync(id);

        if (categoria is null) return false;

        _context.Categorias.Remove(categoria);
        await _context.SaveChangesAsync();

        return true;
    }
}
