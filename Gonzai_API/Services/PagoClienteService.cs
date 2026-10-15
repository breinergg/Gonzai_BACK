using Gonzai_API.Data;
using Gonzai_API.DTOs.PagoCliente;
using Gonzai_API.Extensions;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class PagoClienteService : IPagoClienteService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public PagoClienteService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PagoClienteResponseDto>> GetByVentaIdAsync(int ventaClienteId)
    {
        var pagos = await QueryConDetalle()
            .Where(p => p.VentaClienteId == ventaClienteId)
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();

        return _mapper.Map<IEnumerable<PagoClienteResponseDto>>(pagos);
    }

    public async Task<IEnumerable<PagoClienteResponseDto>> GetByClienteIdAsync(int clienteId)
    {
        var pagos = await QueryConDetalle()
            .Where(p => p.ClienteId == clienteId)
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();

        return _mapper.Map<IEnumerable<PagoClienteResponseDto>>(pagos);
    }

    public async Task<PagoClienteResponseDto?> GetByIdAsync(int id)
    {
        var pago = await QueryConDetalle().FirstOrDefaultAsync(p => p.Id == id);

        return pago is null ? null : _mapper.Map<PagoClienteResponseDto>(pago);
    }

    public async Task<PagoClienteResponseDto> CreateAsync(PagoClienteCreateDto dto, int usuarioId)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync();

        // Bloquear la venta evita que dos abonos simultáneos superen el saldo pendiente.
        var venta = await BloquearVentaAsync(dto.VentaClienteId)
            ?? throw new KeyNotFoundException($"No se encontró la venta con Id {dto.VentaClienteId}.");

        if (venta.ClienteId != dto.ClienteId)
            throw new InvalidOperationException($"La venta {venta.Id} no pertenece al cliente {dto.ClienteId}.");

        if (venta.Estado == EstadosDocumento.Anulada)
            throw new InvalidOperationException($"La venta {venta.Id} está anulada y no admite pagos.");

        if (venta.SaldoPendiente <= 0)
            throw new InvalidOperationException($"La venta {venta.Id} ya está pagada.");

        if (dto.Monto > venta.SaldoPendiente)
            throw new InvalidOperationException(
                $"El abono ({dto.Monto:N0}) supera el saldo pendiente de la venta ({venta.SaldoPendiente:N0}).");

        var pago = new PagoCliente
        {
            VentaClienteId = venta.Id,
            ClienteId      = venta.ClienteId,
            UsuarioId      = usuarioId,
            Monto          = dto.Monto,
            MetodoPago     = dto.MetodoPago,
            Fecha          = FechaNegocio.NormalizarUtc(dto.Fecha),
            Descripcion    = dto.Descripcion
        };

        venta.MontoPagado += dto.Monto;
        venta.RecalcularSaldo();

        _context.PagosCliente.Add(pago);

        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return (await GetByIdAsync(pago.Id))!;
    }

    public async Task<PagoClienteResponseDto?> AnularAsync(int id, string motivo, int usuarioId)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync();

        var pago = await _context.PagosCliente.FirstOrDefaultAsync(p => p.Id == id);

        if (pago is null) return null;

        if (pago.Estado == EstadosPago.Anulado)
            throw new InvalidOperationException($"El pago {id} ya está anulado.");

        var venta = (await BloquearVentaAsync(pago.VentaClienteId))!;

        if (venta.Estado == EstadosDocumento.Anulada)
            throw new InvalidOperationException($"La venta {venta.Id} está anulada; sus pagos ya no cuentan.");

        venta.MontoPagado -= pago.Monto;
        venta.RecalcularSaldo();

        pago.Estado             = EstadosPago.Anulado;
        pago.MotivoAnulacion    = motivo;
        pago.FechaAnulacion     = DateTime.UtcNow;
        pago.UsuarioAnulacionId = usuarioId;

        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return await GetByIdAsync(id);
    }

    private async Task<VentaCliente?> BloquearVentaAsync(int ventaId) =>
        (await _context.VentasCliente
            .FromSql($"SELECT * FROM \"VentaCliente\" WHERE \"Id\" = {ventaId} FOR UPDATE")
            .ToListAsync())
            .FirstOrDefault();

    private IQueryable<PagoCliente> QueryConDetalle() =>
        _context.PagosCliente
            .AsNoTracking()
            .Include(p => p.Cliente)
            .Include(p => p.Usuario)
            .Include(p => p.VentaCliente);
}
