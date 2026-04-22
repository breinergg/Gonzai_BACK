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

    public async Task<ClienteMayorDeudaDto?> GetClienteConMayorDeudaAsync()
    {
        var resultado = await _context.MovimientosCliente
            .AsNoTracking()
            .Where(m => m.TipoMovimiento.ToLower() == "deuda")
            .GroupBy(m => new { m.ClienteId, m.Cliente.Nombre, m.Cliente.Telefono })
            .Select(g => new ClienteMayorDeudaDto
            {
                Id = g.Key.ClienteId,
                Nombre = g.Key.Nombre,
                Telefono = g.Key.Telefono,
                TotalDeuda = g.Sum(m => m.Valor)
            })
            .OrderByDescending(r => r.TotalDeuda)
            .FirstOrDefaultAsync();

        return resultado;
    }

    public async Task<decimal> GetTotalDeudaClientesActivosAsync()
    {
        return await _context.MovimientosCliente
            .AsNoTracking()
            .Where(m => m.TipoMovimiento.ToLower() == "deuda" && m.Cliente.Activo)
            .SumAsync(m => m.Valor);
    }

    public async Task<int> GetClientesActivosConDeudaCountAsync()
    {
        return await _context.MovimientosCliente
            .AsNoTracking()
            .Where(m => m.TipoMovimiento.ToLower() == "deuda" && m.Cliente.Activo)
            .Select(m => m.ClienteId)
            .Distinct()
            .CountAsync();
    }

    public async Task<int> GetClientesActivosCountAsync()
    {
        return await _context.Clientes
            .AsNoTracking()
            .CountAsync(c => c.Activo);
    }

    public async Task<ClienteSaldoDto?> GetSaldoByClienteIdAsync(int clienteId)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clienteId);

        if (cliente is null) return null;

        var movimientos = await _context.MovimientosCliente
            .AsNoTracking()
            .Where(m => m.ClienteId == clienteId)
            .ToListAsync();

        var totalDeuda = movimientos
            .Where(m => m.TipoMovimiento.ToLower() == "deuda")
            .Sum(m => m.Valor);

        var totalAbonos = movimientos
            .Where(m => m.TipoMovimiento.ToLower() == "abono")
            .Sum(m => m.Valor);

        return new ClienteSaldoDto
        {
            ClienteId = cliente.Id,
            Nombre = cliente.Nombre,
            TotalDeuda = totalDeuda,
            TotalAbonos = totalAbonos,
            Saldo = totalDeuda - totalAbonos
        };
    }

}
