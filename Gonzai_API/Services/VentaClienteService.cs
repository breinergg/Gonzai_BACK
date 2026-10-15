using Gonzai_API.Data;
using Gonzai_API.DTOs.VentaCliente;
using Gonzai_API.Extensions;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class VentaClienteService : IVentaClienteService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IInventarioService _inventario;

    public VentaClienteService(AppDbContext context, IMapper mapper, IInventarioService inventario)
    {
        _context    = context;
        _mapper     = mapper;
        _inventario = inventario;
    }

    public async Task<IEnumerable<VentaClienteResponseDto>> GetAllAsync(
        int? clienteId = null, DateOnly? desde = null, DateOnly? hasta = null, bool soloPendientes = false, int? limit = null)
    {
        var query = QueryConDetalle();

        if (clienteId.HasValue)
            query = query.Where(v => v.ClienteId == clienteId.Value);

        if (desde.HasValue || hasta.HasValue)
        {
            var (desdeUtc, hastaUtc) = FechaNegocio.RangoUtc(desde ?? DateOnly.MinValue, hasta ?? FechaNegocio.Hoy());
            query = query.Where(v => v.Fecha >= desdeUtc && v.Fecha < hastaUtc);
        }

        if (soloPendientes)
            query = query.Where(v => v.Estado == EstadosDocumento.Activa && v.SaldoPendiente > 0);

        query = query.OrderByDescending(v => v.Fecha).ThenByDescending(v => v.Id);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        var ventas = await query.ToListAsync();

        return _mapper.Map<IEnumerable<VentaClienteResponseDto>>(ventas);
    }

    public async Task<VentaClienteResponseDto?> GetByIdAsync(int id)
    {
        var venta = await QueryConDetalle().FirstOrDefaultAsync(v => v.Id == id);

        return venta is null ? null : _mapper.Map<VentaClienteResponseDto>(venta);
    }

    public async Task<VentaClienteResponseDto> CreateAsync(VentaClienteCreateDto dto, int usuarioId)
    {
        var clienteNombre = await _context.Clientes
            .Where(c => c.Id == dto.ClienteId && c.Activo)
            .Select(c => c.Nombre)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"No se encontró un cliente activo con Id {dto.ClienteId}.");

        if (dto.MontoPagado > 0 && string.IsNullOrWhiteSpace(dto.MetodoPago))
            throw new InvalidOperationException("Debe indicar el método de pago cuando el cliente paga algo en el momento.");

        var fecha = FechaNegocio.NormalizarUtc(dto.Fecha);

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        var variantes = await _inventario.BloquearVariantesAsync(dto.Detalles.Select(d => d.ProductoVarianteId));

        var venta = new VentaCliente
        {
            ClienteId   = dto.ClienteId,
            UsuarioId   = usuarioId,
            Fecha       = fecha,
            Descripcion = dto.Descripcion
        };

        foreach (var linea in dto.Detalles)
        {
            var variante = variantes[linea.ProductoVarianteId];

            var detalle = new VentaClienteDetalle
            {
                ProductoVariante            = variante,
                CompraProveedorSolicitadaId = linea.CompraProveedorId,
                Cantidad                    = linea.Cantidad,
                PrecioVentaUnitario         = linea.PrecioVentaUnitario,
                Subtotal                    = linea.Cantidad * linea.PrecioVentaUnitario
            };

            var consumos = await _inventario.RegistrarSalidaAsync(new SalidaInventario(
                variante, linea.Cantidad, MotivosMovimientoInventario.VentaCliente, usuarioId, fecha,
                $"Venta a {clienteNombre}", CompraProveedorId: linea.CompraProveedorId, Venta: venta));

            foreach (var consumo in consumos)
            {
                detalle.Consumos.Add(new VentaDetalleExistencia
                {
                    ExistenciaLote = consumo.Existencia,
                    Cantidad       = consumo.Cantidad,
                    CostoUnitario  = consumo.Existencia.CostoUnitario
                });
            }

            detalle.CostoTotal = detalle.Consumos.Sum(c => c.Cantidad * c.CostoUnitario);
            venta.Detalles.Add(detalle);
        }

        venta.Total = venta.Detalles.Sum(d => d.Subtotal);

        if (dto.MontoPagado > venta.Total)
            throw new InvalidOperationException(
                $"El monto pagado ({dto.MontoPagado:N0}) no puede ser mayor al total de la venta ({venta.Total:N0}).");

        venta.MontoPagado = dto.MontoPagado;
        venta.RecalcularSaldo();

        if (dto.MontoPagado > 0)
        {
            venta.Pagos.Add(new PagoCliente
            {
                ClienteId   = dto.ClienteId,
                UsuarioId   = usuarioId,
                Monto       = dto.MontoPagado,
                MetodoPago  = dto.MetodoPago!,
                Fecha       = fecha,
                Descripcion = "Pago al momento de la venta"
            });
        }

        _context.VentasCliente.Add(venta);

        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return (await GetByIdAsync(venta.Id))!;
    }

    public async Task<VentaClienteResponseDto?> AnularAsync(int id, string motivo, int usuarioId)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync();

        var venta = (await _context.VentasCliente
            .FromSql($"SELECT * FROM \"VentaCliente\" WHERE \"Id\" = {id} FOR UPDATE")
            .ToListAsync())
            .FirstOrDefault();

        if (venta is null) return null;

        if (venta.Estado == EstadosDocumento.Anulada)
            throw new InvalidOperationException($"La venta {id} ya está anulada.");

        var varianteIds = await _context.VentasClienteDetalle
            .Where(d => d.VentaClienteId == id)
            .Select(d => d.ProductoVarianteId)
            .ToListAsync();

        var variantes = await _inventario.BloquearVariantesAsync(varianteIds, exigirActivas: false);

        var consumos = await _context.VentasDetalleExistencia
            .Include(c => c.ExistenciaLote)
            .Include(c => c.VentaClienteDetalle)
            .Where(c => c.VentaClienteDetalle.VentaClienteId == id)
            .ToListAsync();

        // Cada unidad vuelve a la misma existencia (y por tanto al mismo lote) de la que salió.
        foreach (var consumo in consumos)
        {
            _inventario.Reintegrar(
                variantes[consumo.VentaClienteDetalle.ProductoVarianteId], consumo.ExistenciaLote, consumo.Cantidad,
                usuarioId, motivo, venta);
        }

        // Anular la venta implica devolver lo pagado: sus pagos dejan de contar.
        var pagos = await _context.PagosCliente
            .Where(p => p.VentaClienteId == id && p.Estado == EstadosPago.Activo)
            .ToListAsync();

        foreach (var pago in pagos)
        {
            pago.Estado             = EstadosPago.Anulado;
            pago.MotivoAnulacion    = $"Venta anulada: {motivo}";
            pago.FechaAnulacion     = DateTime.UtcNow;
            pago.UsuarioAnulacionId = usuarioId;
        }

        venta.Estado             = EstadosDocumento.Anulada;
        venta.MotivoAnulacion    = motivo;
        venta.FechaAnulacion     = DateTime.UtcNow;
        venta.UsuarioAnulacionId = usuarioId;
        venta.MontoPagado        = 0;
        venta.SaldoPendiente     = 0;

        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return await GetByIdAsync(id);
    }

    public async Task<VentaMensualResumenDto> GetResumenMensualAsync()
    {
        var hoy               = FechaNegocio.Hoy();
        var inicioMesActual   = new DateOnly(hoy.Year, hoy.Month, 1);
        var inicioMesAnterior = inicioMesActual.AddMonths(-1);

        var totalActual   = await TotalVendidoAsync(inicioMesActual, hoy);
        var totalAnterior = await TotalVendidoAsync(inicioMesAnterior, inicioMesActual.AddDays(-1));

        decimal porcentajeCambio = 0m;
        if (totalAnterior != 0)
            porcentajeCambio = Math.Round(((totalActual - totalAnterior) / totalAnterior) * 100, 2);

        var cultura = new System.Globalization.CultureInfo("es-ES");

        return new VentaMensualResumenDto
        {
            Mes              = hoy.ToString("MMM yyyy", cultura),
            MesAnterior      = inicioMesAnterior.ToString("MMM yyyy", cultura),
            TotalMesActual   = totalActual,
            TotalMesAnterior = totalAnterior,
            PorcentajeCambio = Math.Abs(porcentajeCambio),
            EsAumento        = porcentajeCambio >= 0
        };
    }

    private async Task<decimal> TotalVendidoAsync(DateOnly desde, DateOnly hasta)
    {
        var (desdeUtc, hastaUtc) = FechaNegocio.RangoUtc(desde, hasta);

        return await _context.VentasCliente
            .AsNoTracking()
            .Where(v => v.Estado == EstadosDocumento.Activa && v.Fecha >= desdeUtc && v.Fecha < hastaUtc)
            .SumAsync(v => (decimal?)v.Total) ?? 0m;
    }

    private IQueryable<VentaCliente> QueryConDetalle() =>
        _context.VentasCliente
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.Usuario)
            .Include(v => v.Detalles).ThenInclude(d => d.ProductoVariante).ThenInclude(pv => pv.Producto)
            .Include(v => v.Detalles).ThenInclude(d => d.CompraProveedorSolicitada)
            .Include(v => v.Detalles).ThenInclude(d => d.Consumos).ThenInclude(c => c.ExistenciaLote)
                .ThenInclude(e => e.CompraProveedorDetalle).ThenInclude(cd => cd!.CompraProveedor)
            .Include(v => v.Pagos)
            .AsSplitQuery();
}
