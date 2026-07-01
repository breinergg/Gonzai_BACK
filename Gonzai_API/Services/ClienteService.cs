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
        // Agrupa todos los movimientos por cliente y calcula saldo real:
        // saldo = suma(deudas) - suma(abonos)
        // Solo retorna clientes activos con saldo > 0
        var resultado = await _context.MovimientosCliente
            .AsNoTracking()
            .Where(m => m.Cliente.Activo)
            .GroupBy(m => new { m.ClienteId, m.Cliente.Nombre, m.Cliente.Telefono })
            .Select(g => new ClienteMayorDeudaDto
            {
                Id       = g.Key.ClienteId,
                Nombre   = g.Key.Nombre,
                Telefono = g.Key.Telefono,
                TotalDeuda = g.Sum(m =>
                    m.TipoMovimiento.ToLower() == "deuda"  ?  m.Valor :
                    m.TipoMovimiento.ToLower() == "abono"  ? -m.Valor : 0)
            })
            .Where(r => r.TotalDeuda > 0)
            .OrderByDescending(r => r.TotalDeuda)
            .FirstOrDefaultAsync();

        return resultado;
    }

    public async Task<decimal> GetTotalDeudaClientesActivosAsync()
    {
        // Suma neta: deudas menos abonos de todos los clientes activos
        var movimientos = await _context.MovimientosCliente
            .AsNoTracking()
            .Where(m => m.Cliente.Activo)
            .Select(m => new { m.TipoMovimiento, m.Valor })
            .ToListAsync();

        return movimientos.Sum(m =>
            m.TipoMovimiento.ToLower() == "deuda" ?  m.Valor :
            m.TipoMovimiento.ToLower() == "abono" ? -m.Valor : 0);
    }

    public async Task<int> GetClientesActivosConDeudaCountAsync()
    {
        // Cuenta solo clientes cuyo saldo neto (deuda - abonos) es mayor a cero
        var saldosPorCliente = await _context.MovimientosCliente
            .AsNoTracking()
            .Where(m => m.Cliente.Activo)
            .GroupBy(m => m.ClienteId)
            .Select(g => new
            {
                ClienteId = g.Key,
                Saldo     = g.Sum(m =>
                    m.TipoMovimiento.ToLower() == "deuda" ?  m.Valor :
                    m.TipoMovimiento.ToLower() == "abono" ? -m.Valor : 0)
            })
            .ToListAsync();

        return saldosPorCliente.Count(s => s.Saldo > 0);
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

    public async Task<IEnumerable<ClienteResponseDto>> SearchActivosByNombreAsync(string filtro, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(filtro))
            return await GetAllActivosAsync();

        var clientes = await _context.Clientes
            .AsNoTracking()
            .Where(c => c.Activo && EF.Functions.ILike(c.Nombre, $"%{filtro.Trim()}%"))
            .OrderBy(c => c.Nombre)
            .Take(limit)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ClienteResponseDto>>(clientes);
    }

    public async Task<IEnumerable<ClienteSaldoDto>> SearchSaldosByNombreAsync(string nombre, int limit = 10)
    {
        var clientes = await _context.Clientes
            .AsNoTracking()
            .Where(c => c.Activo && EF.Functions.ILike(c.Nombre, $"%{nombre.Trim()}%"))
            .OrderBy(c => c.Nombre)
            .Take(limit)
            .ToListAsync();

        var saldos = new List<ClienteSaldoDto>();

        foreach (var cliente in clientes)
        {
            var saldo = await GetSaldoByClienteIdAsync(cliente.Id);
            if (saldo is not null)
                saldos.Add(saldo);
        }

        return saldos;
    }

}
