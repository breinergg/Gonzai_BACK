using Gonzai_API.Data;
using Gonzai_API.DTOs.Lote;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class LoteService : ILoteService
{
    private readonly AppDbContext _context;

    public LoteService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LoteResumenDto>> GetAllAsync(int? proveedorId = null, bool soloConStock = false)
    {
        var query = QueryCompras();

        if (proveedorId.HasValue)
            query = query.Where(c => c.ProveedorId == proveedorId.Value);

        if (soloConStock)
            query = query.Where(c => c.Detalles.Any(d => d.Existencia != null && d.Existencia.CantidadDisponible > 0));

        var compras = await query.OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id).ToListAsync();
        var ventas  = await VentasPorDetalleAsync(compras.Select(c => c.Id).ToList());

        return compras.Select(c => Construir<LoteResumenDto>(c, ventas, out _)).ToList();
    }

    public async Task<LoteDetalleDto?> GetByIdAsync(int compraProveedorId) =>
        await GetDetalleAsync(QueryCompras().Where(c => c.Id == compraProveedorId));

    public async Task<LoteDetalleDto?> GetByCodigoAsync(string codigo) =>
        await GetDetalleAsync(QueryCompras().Where(c => c.Codigo == codigo.Trim().ToUpper()));

    public async Task<IEnumerable<ExistenciaDisponibleDto>> GetDisponiblesAsync(int? productoId, int? productoVarianteId)
    {
        var query = _context.ExistenciasLote
            .AsNoTracking()
            .Where(e => e.CantidadDisponible > 0);

        if (productoVarianteId.HasValue)
            query = query.Where(e => e.ProductoVarianteId == productoVarianteId.Value);

        if (productoId.HasValue)
            query = query.Where(e => e.ProductoVariante.ProductoId == productoId.Value);

        var existencias = await query
            .Include(e => e.ProductoVariante).ThenInclude(v => v.Producto)
            .Include(e => e.CompraProveedorDetalle).ThenInclude(d => d!.CompraProveedor).ThenInclude(c => c.Proveedor)
            .OrderBy(e => e.ProductoVarianteId)
            .ThenBy(e => e.FechaIngreso)
            .ThenBy(e => e.Id)
            .ToListAsync();

        return existencias.Select(e =>
        {
            var compra = e.CompraProveedorDetalle?.CompraProveedor;
            return new ExistenciaDisponibleDto
            {
                ExistenciaId       = e.Id,
                ProductoVarianteId = e.ProductoVarianteId,
                VarianteNombre     = e.ProductoVariante.NombreCompleto,
                CompraProveedorId  = compra?.Id,
                LoteCodigo         = compra?.Codigo,
                LoteNombre         = compra?.Nombre,
                ProveedorNombre    = compra?.Proveedor.Nombre,
                Origen             = e.Origen,
                FechaIngreso       = e.FechaIngreso,
                CantidadDisponible = e.CantidadDisponible,
                CostoUnitario      = e.CostoUnitario
            };
        }).ToList();
    }

    private async Task<LoteDetalleDto?> GetDetalleAsync(IQueryable<CompraProveedor> query)
    {
        var compra = await query.FirstOrDefaultAsync();
        if (compra is null) return null;

        var ventas  = await VentasPorDetalleAsync([compra.Id]);
        var detalle = Construir<LoteDetalleDto>(compra, ventas, out var lineas);

        detalle.NumeroFactura = compra.NumeroFactura;
        detalle.Descripcion   = compra.Descripcion;
        detalle.Lineas        = lineas;

        return detalle;
    }

    private sealed record VentasDetalle(int Unidades, decimal Ingresos, decimal Costo);

    /// <summary>Unidades, ingresos y costo vendidos por línea de compra, solo de ventas activas.</summary>
    private async Task<Dictionary<int, VentasDetalle>> VentasPorDetalleAsync(List<int> compraIds)
    {
        if (compraIds.Count == 0) return [];

        var filas = await _context.VentasDetalleExistencia
            .AsNoTracking()
            .Where(c => c.ExistenciaLote.CompraProveedorDetalleId != null
                        && compraIds.Contains(c.ExistenciaLote.CompraProveedorDetalle!.CompraProveedorId)
                        && c.VentaClienteDetalle.VentaCliente.Estado == EstadosDocumento.Activa)
            .GroupBy(c => c.ExistenciaLote.CompraProveedorDetalleId!.Value)
            .Select(g => new
            {
                DetalleId = g.Key,
                Unidades  = g.Sum(x => x.Cantidad),
                Ingresos  = g.Sum(x => x.Cantidad * x.VentaClienteDetalle.PrecioVentaUnitario),
                Costo     = g.Sum(x => x.Cantidad * x.CostoUnitario)
            })
            .ToListAsync();

        return filas.ToDictionary(f => f.DetalleId,
            f => new VentasDetalle(f.Unidades, Math.Round(f.Ingresos, 2), Math.Round(f.Costo, 2)));
    }

    private static T Construir<T>(CompraProveedor compra, Dictionary<int, VentasDetalle> ventas, out List<LoteLineaDto> lineas)
        where T : LoteResumenDto, new()
    {
        lineas = compra.Detalles.OrderBy(d => d.Id).Select(d =>
        {
            var vendido    = ventas.GetValueOrDefault(d.Id) ?? new VentasDetalle(0, 0m, 0m);
            var disponible = d.Existencia?.CantidadDisponible ?? 0;
            var variante   = d.ProductoVariante;

            return new LoteLineaDto
            {
                CompraProveedorDetalleId = d.Id,
                ExistenciaId             = d.Existencia?.Id,
                ProductoVarianteId       = variante.Id,
                ProductoId               = variante.ProductoId,
                ProductoNombre           = variante.Producto.Nombre,
                VarianteNombre           = variante.NombreCompleto,
                Talla                    = variante.Talla,
                Color                    = variante.Color,
                CostoUnitario            = d.CostoUnitario,
                Comprado                 = d.Cantidad,
                Vendido                  = vendido.Unidades,
                Disponible               = disponible,
                Ajustado                 = Math.Max(0, d.Cantidad - vendido.Unidades - disponible),
                Ingresos                 = vendido.Ingresos,
                CostoVendido             = vendido.Costo,
                GananciaBruta            = vendido.Ingresos - vendido.Costo
            };
        }).ToList();

        var comprado  = lineas.Sum(l => l.Comprado);
        var ingresos  = lineas.Sum(l => l.Ingresos);
        var costo     = lineas.Sum(l => l.CostoVendido);
        var inversion = compra.SubtotalProductos + compra.CostoEnvio;

        return new T
        {
            CompraProveedorId         = compra.Id,
            Codigo                    = compra.Codigo,
            Nombre                    = compra.Nombre,
            ProveedorId               = compra.ProveedorId,
            ProveedorNombre           = compra.Proveedor.Nombre,
            Fecha                     = compra.Fecha,
            Estado                    = compra.Estado,
            UnidadesCompradas         = comprado,
            UnidadesVendidas          = lineas.Sum(l => l.Vendido),
            UnidadesDisponibles       = lineas.Sum(l => l.Disponible),
            UnidadesAjustadas         = lineas.Sum(l => l.Ajustado),
            SubtotalProductos         = compra.SubtotalProductos,
            CostoEnvio                = compra.CostoEnvio,
            InversionTotal            = inversion,
            Ingresos                  = ingresos,
            CostoVendido              = costo,
            GananciaBruta             = ingresos - costo,
            GananciaNeta              = ingresos - costo - compra.CostoEnvio,
            ValorInventarioDisponible = lineas.Sum(l => l.Disponible * l.CostoUnitario),
            PorcentajeVendido         = comprado == 0 ? 0 : Math.Round(lineas.Sum(l => l.Vendido) * 100m / comprado, 2),
            PorcentajeRecuperado      = inversion == 0 ? 0 : Math.Round(ingresos * 100m / inversion, 2),
            InversionRecuperada       = ingresos >= inversion
        };
    }

    private IQueryable<CompraProveedor> QueryCompras() =>
        _context.ComprasProveedor
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .Include(c => c.Detalles).ThenInclude(d => d.ProductoVariante).ThenInclude(v => v.Producto)
            .Include(c => c.Detalles).ThenInclude(d => d.Existencia)
            .AsSplitQuery();
}
