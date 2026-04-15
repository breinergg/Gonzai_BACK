using Gonzai_API.Data;
using Gonzai_API.DTOs.Cliente;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class ClienteService : IClienteService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public ClienteService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ClienteResponseDto>> GetAllAsync()
    {
        var clientes = await _context.Clientes
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ClienteResponseDto>>(clientes);
    }

    public async Task<IEnumerable<ClienteResponseDto>> GetAllActivosAsync()
    {
        var clientes = await _context.Clientes
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ClienteResponseDto>>(clientes);
    }

    public async Task<ClienteResponseDto?> GetByIdAsync(int id)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cliente is null) return null;

        return _mapper.Map<ClienteResponseDto>(cliente);
    }

    public async Task<ClienteResponseDto> CreateAsync(ClienteCreateDto dto)
    {
        var cliente = _mapper.Map<Cliente>(dto);

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        return _mapper.Map<ClienteResponseDto>(cliente);
    }

    public async Task<ClienteResponseDto?> UpdateAsync(int id, ClienteUpdateDto dto)
    {
        var cliente = await _context.Clientes.FindAsync(id);

        if (cliente is null) return null;

        _mapper.Map(dto, cliente);
        await _context.SaveChangesAsync();

        return _mapper.Map<ClienteResponseDto>(cliente);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id);

        if (cliente is null) return false;

        // Soft delete: preserva historial de movimientos del cliente
        cliente.Activo = false;
        await _context.SaveChangesAsync();

        return true;
    }
}
