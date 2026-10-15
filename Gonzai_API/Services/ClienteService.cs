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

        // Soft delete: preserva historial de ventas y pagos del cliente
        cliente.Activo = false;
        await _context.SaveChangesAsync();

        return true;
    }

    // La deuda vive en cada venta: saldo del cliente = suma de SaldoPendiente de sus ventas activas.
    private IQueryable<VentaCliente> VentasConSaldo() =>
        _context.VentasCliente
            .AsNoTracking()
            .Where(v => v.Estado == EstadosDocumento.Activa && v.SaldoPendiente > 0 && v.Cliente.Activo);

    public async Task<ClienteMayorDeudaDto?> GetClienteConMayorDeudaAsync()
    {
        return await VentasConSaldo()
            .GroupBy(v => new { v.ClienteId, v.Cliente.Nombre, v.Cliente.Telefono })
            .Select(g => new ClienteMayorDeudaDto
            {
                Id         = g.Key.ClienteId,
                Nombre     = g.Key.Nombre,
                Telefono   = g.Key.Telefono,
                TotalDeuda = g.Sum(v => v.SaldoPendiente)
            })
            .OrderByDescending(r => r.TotalDeuda)
            .FirstOrDefaultAsync();
    }

    public async Task<decimal> GetTotalDeudaClientesActivosAsync()
    {
        return await VentasConSaldo().SumAsync(v => (decimal?)v.SaldoPendiente) ?? 0m;
    }

    public async Task<int> GetClientesActivosConDeudaCountAsync()
    {
        return await VentasConSaldo()
            .Select(v => v.ClienteId)
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

        var ventas = await _context.VentasCliente
            .AsNoTracking()
            .Where(v => v.ClienteId == clienteId && v.Estado == EstadosDocumento.Activa)
            .OrderBy(v => v.Fecha)
            .ToListAsync();

        return new ClienteSaldoDto
        {
            ClienteId     = cliente.Id,
            Nombre        = cliente.Nombre,
            TotalComprado = ventas.Sum(v => v.Total),
            TotalPagado   = ventas.Sum(v => v.MontoPagado),
            Saldo         = ventas.Sum(v => v.SaldoPendiente),
            VentasPendientes = ventas
                .Where(v => v.SaldoPendiente > 0)
                .Select(v => new VentaPendienteDto
                {
                    VentaClienteId = v.Id,
                    Fecha          = v.Fecha,
                    Total          = v.Total,
                    MontoPagado    = v.MontoPagado,
                    SaldoPendiente = v.SaldoPendiente,
                    EstadoPago     = v.EstadoPago
                })
                .ToList()
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
