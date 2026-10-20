using Gonzai_API.Data;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class InventarioService : IInventarioService
{
    private readonly AppDbContext _context;

    public InventarioService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<int, ProductoVariante>> BloquearVariantesAsync(
        IEnumerable<int> varianteIds, bool exigirActivas = true, CancellationToken ct = default)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("El bloqueo de variantes requiere una transacción activa.");

        var ids = varianteIds.Distinct().OrderBy(id => id).ToArray();

        // Ordenar por Id evita interbloqueos entre dos operaciones que bloquean las mismas variantes.
        var variantes = await _context.ProductoVariantes
            .FromSql($"SELECT * FROM \"ProductoVariante\" WHERE \"Id\" = ANY({ids}) ORDER BY \"Id\" FOR UPDATE")
            .ToListAsync(ct);

        var faltantes = ids.Except(variantes.Select(v => v.Id)).ToList();
        if (faltantes.Count > 0)
            throw new KeyNotFoundException($"No se encontraron las variantes con Id: {string.Join(", ", faltantes)}.");

        var productoIds = variantes.Select(v => v.ProductoId).Distinct().ToList();
        await _context.Productos.Where(p => productoIds.Contains(p.Id)).LoadAsync(ct);

        if (exigirActivas)
        {
            var inactivas = variantes.Where(v => !v.Activo || !v.Producto.Activo).Select(v => v.NombreCompleto).ToList();
            if (inactivas.Count > 0)
                throw new InvalidOperationException($"Los siguientes productos están inactivos: {string.Join(", ", inactivas)}.");
        }

        return variantes.ToDictionary(v => v.Id);
    }

    public ExistenciaLote RegistrarEntrada(EntradaInventario entrada)
    {
        var existencia = new ExistenciaLote
        {
            ProductoVariante       = entrada.Variante,
            CompraProveedorDetalle = entrada.CompraDetalle,
            Origen                 = entrada.Motivo,
            CantidadInicial        = entrada.Cantidad,
            CantidadDisponible     = entrada.Cantidad,
            CostoUnitario          = entrada.CostoUnitario,
            FechaIngreso           = entrada.Fecha
        };

        _context.ExistenciasLote.Add(existencia);
        entrada.Variante.StockActual += entrada.Cantidad;

        AgregarMovimiento(entrada.Variante, existencia, TiposMovimientoInventario.Entrada, entrada.Motivo,
            entrada.Cantidad, entrada.CostoUnitario, entrada.UsuarioId, entrada.Fecha, entrada.Descripcion,
            compra: entrada.Compra);

        return existencia;
    }

    public async Task<List<ConsumoExistencia>> RegistrarSalidaAsync(SalidaInventario salida, CancellationToken ct = default)
    {
        var variante = salida.Variante;

        List<ConsumoExistencia> consumos = salida.ExistenciaLoteId is { } existenciaId
            ? [await ConsumirExistenciaEspecificaAsync(variante, existenciaId, salida.Cantidad, ct)]
            : await ConsumirFifoAsync(variante, salida.Cantidad, salida.CompraProveedorId, ct);

        foreach (var consumo in consumos)
        {
            consumo.Existencia.CantidadDisponible -= consumo.Cantidad;
            variante.StockActual                  -= consumo.Cantidad;

            AgregarMovimiento(variante, consumo.Existencia, TiposMovimientoInventario.Salida, salida.Motivo,
                consumo.Cantidad, consumo.Existencia.CostoUnitario, salida.UsuarioId, salida.Fecha, salida.Descripcion,
                compra: consumo.Existencia.CompraProveedorDetalle?.CompraProveedor, venta: salida.Venta);
        }

        return consumos;
    }

    public void Reintegrar(
        ProductoVariante variante, ExistenciaLote existencia, int cantidad, int usuarioId, string? descripcion, VentaCliente venta)
    {
        existencia.CantidadDisponible += cantidad;
        variante.StockActual          += cantidad;

        AgregarMovimiento(variante, existencia, TiposMovimientoInventario.Entrada, MotivosMovimientoInventario.AnulacionVenta,
            cantidad, existencia.CostoUnitario, usuarioId, DateTime.UtcNow, descripcion, venta: venta);
    }

    public void RetirarExistenciaCompleta(
        ProductoVariante variante, ExistenciaLote existencia, int usuarioId, string? descripcion, CompraProveedor compra)
    {
        if (existencia.CantidadDisponible != existencia.CantidadInicial)
            throw new InvalidOperationException(
                $"No se puede anular: del lote {compra.Codigo} ya salieron " +
                $"{existencia.CantidadInicial - existencia.CantidadDisponible} unidades de '{variante.NombreCompleto}'. " +
                "Anule primero las ventas o haga un ajuste.");

        var cantidad = existencia.CantidadDisponible;
        existencia.CantidadDisponible = 0;
        variante.StockActual         -= cantidad;

        AgregarMovimiento(variante, existencia, TiposMovimientoInventario.Salida, MotivosMovimientoInventario.AnulacionCompra,
            cantidad, existencia.CostoUnitario, usuarioId, DateTime.UtcNow, descripcion, compra: compra);
    }

    private async Task<ConsumoExistencia> ConsumirExistenciaEspecificaAsync(
        ProductoVariante variante, int existenciaId, int cantidad, CancellationToken ct)
    {
        var existencia = await _context.ExistenciasLote
            .Include(e => e.CompraProveedorDetalle)
                .ThenInclude(d => d!.CompraProveedor)
            .FirstOrDefaultAsync(e => e.Id == existenciaId, ct)
            ?? throw new KeyNotFoundException($"No se encontró la existencia con Id {existenciaId}.");

        if (existencia.ProductoVarianteId != variante.Id)
            throw new InvalidOperationException(
                $"La existencia {existenciaId} no pertenece a '{variante.NombreCompleto}'.");

        if (existencia.CantidadDisponible < cantidad)
            throw new InvalidOperationException(
                $"La existencia {existenciaId} de '{variante.NombreCompleto}' solo tiene {existencia.CantidadDisponible} " +
                $"unidades disponibles y se solicitaron {cantidad}.");

        return new ConsumoExistencia(existencia, cantidad);
    }

    private async Task<List<ConsumoExistencia>> ConsumirFifoAsync(
        ProductoVariante variante, int cantidad, int? compraProveedorId, CancellationToken ct)
    {
        var query = _context.ExistenciasLote
            .Include(e => e.CompraProveedorDetalle)
                .ThenInclude(d => d!.CompraProveedor)
            .Where(e => e.ProductoVarianteId == variante.Id);

        string? codigoLote = null;
        if (compraProveedorId is { } compraId)
        {
            codigoLote = await _context.ComprasProveedor
                .Where(c => c.Id == compraId)
                .Select(c => c.Codigo)
                .FirstOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException($"No se encontró el lote (compra) con Id {compraId}.");

            query = query.Where(e => e.CompraProveedorDetalle != null
                                     && e.CompraProveedorDetalle.CompraProveedorId == compraId);
        }

        var existencias = await query
            .Where(e => e.CantidadDisponible > 0)
            .OrderBy(e => e.FechaIngreso)
            .ThenBy(e => e.Id)
            .ToListAsync(ct);

        // Si otra línea de la misma operación ya consumió una existencia, la base de datos aún no
        // lo sabe pero la entidad en memoria sí: se filtra de nuevo con el valor en memoria.
        existencias = existencias.Where(e => e.CantidadDisponible > 0).ToList();

        var disponible = existencias.Sum(e => e.CantidadDisponible);
        if (disponible < cantidad)
        {
            if (codigoLote is not null && disponible == 0 && !await query.AnyAsync(ct))
                throw new InvalidOperationException(
                    $"El lote {codigoLote} no incluye '{variante.NombreCompleto}'.");

            var donde = codigoLote is null ? string.Empty : $" en el lote {codigoLote}";
            throw new InvalidOperationException(
                $"Stock insuficiente de '{variante.NombreCompleto}'{donde}. Disponible: {disponible}, solicitado: {cantidad}.");
        }

        var consumos = new List<ConsumoExistencia>();
        var restante = cantidad;

        foreach (var existencia in existencias)
        {
            if (restante == 0) break;

            var tomar = Math.Min(existencia.CantidadDisponible, restante);
            consumos.Add(new ConsumoExistencia(existencia, tomar));
            restante -= tomar;
        }

        return consumos;
    }

    private void AgregarMovimiento(
        ProductoVariante variante, ExistenciaLote existencia, string tipo, string motivo, int cantidad,
        decimal costoUnitario, int usuarioId, DateTime fecha, string? descripcion,
        CompraProveedor? compra = null, VentaCliente? venta = null)
    {
        _context.MovimientosInventario.Add(new MovimientoInventario
        {
            ProductoVariante = variante,
            ExistenciaLote   = existencia,
            TipoMovimiento   = tipo,
            Motivo           = motivo,
            Cantidad         = cantidad,
            CostoUnitario    = costoUnitario,
            StockResultante  = variante.StockActual,
            CompraProveedor  = compra,
            VentaCliente     = venta,
            UsuarioId        = usuarioId,
            Fecha            = fecha,
            Descripcion      = descripcion
        });
    }
}
