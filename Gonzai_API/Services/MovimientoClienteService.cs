using Gonzai_API.Data;
using Gonzai_API.DTOs.MovimientoCliente;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class MovimientoClienteService : IMovimientoClienteService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public MovimientoClienteService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<MovimientoClienteResponseDto>> GetAllAsync()
    {
        var movimientos = await _context.MovimientosCliente
            .AsNoTracking()
            .Include(m => m.Cliente)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();

        return _mapper.Map<IEnumerable<MovimientoClienteResponseDto>>(movimientos);
    }

    public async Task<IEnumerable<MovimientoClienteResponseDto>> GetByClienteIdAsync(int clienteId)
    {
        var movimientos = await _context.MovimientosCliente
            .AsNoTracking()
            .Include(m => m.Cliente)
            .Where(m => m.ClienteId == clienteId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();

        return _mapper.Map<IEnumerable<MovimientoClienteResponseDto>>(movimientos);
    }

    public async Task<MovimientoClienteResponseDto?> GetByIdAsync(int id)
    {
        var movimiento = await _context.MovimientosCliente
            .AsNoTracking()
            .Include(m => m.Cliente)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movimiento is null) return null;

        return _mapper.Map<MovimientoClienteResponseDto>(movimiento);
    }

    public async Task<MovimientoClienteResponseDto> CreateAsync(MovimientoClienteCreateDto dto)
    {
        var clienteExiste = await _context.Clientes
            .AnyAsync(c => c.Id == dto.ClienteId && c.Activo);

        if (!clienteExiste)
            throw new KeyNotFoundException($"No se encontró un cliente activo con Id {dto.ClienteId}.");

        var movimiento = _mapper.Map<MovimientoCliente>(dto);

        _context.MovimientosCliente.Add(movimiento);
        await _context.SaveChangesAsync();

        // Recargar con el nombre del cliente para el response
        await _context.Entry(movimiento)
            .Reference(m => m.Cliente)
            .LoadAsync();

        return _mapper.Map<MovimientoClienteResponseDto>(movimiento);
    }
}
