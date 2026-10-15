using Gonzai_API.Data;
using Gonzai_API.DTOs.CompraProveedor;
using Gonzai_API.Extensions;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class CompraProveedorService : ICompraProveedorService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IInventarioService _inventario;

    public CompraProveedorService(AppDbContext context, IMapper mapper, IInventarioService inventario)
    {
        _context    = context;
        _mapper     = mapper;
        _inventario = inventario;
    }

    public async Task<IEnumerable<CompraProveedorResponseDto>> GetAllAsync(
        int? proveedorId = null, DateOnly? desde = null, DateOnly? hasta = null)
    {
        var query = QueryConDetalle();

        if (proveedorId.HasValue)
            query = query.Where(c => c.ProveedorId == proveedorId.Value);

        if (desde.HasValue || hasta.HasValue)
        {
            var (desdeUtc, hastaUtc) = FechaNegocio.RangoUtc(desde ?? DateOnly.MinValue, hasta ?? FechaNegocio.Hoy());
            query = query.Where(c => c.Fecha >= desdeUtc && c.Fecha < hastaUtc);
        }

        var compras = await query.OrderByDescending(c => c.Fecha).ToListAsync();

        return _mapper.Map<IEnumerable<CompraProveedorResponseDto>>(compras);
    }

    public async Task<CompraProveedorResponseDto?> GetByIdAsync(int id)
    {
        var compra = await QueryConDetalle().FirstOrDefaultAsync(c => c.Id == id);

        return compra is null ? null : _mapper.Map<CompraProveedorResponseDto>(compra);
    }

    public async Task<CompraProveedorResponseDto> CreateAsync(CompraProveedorCreateDto dto, int usuarioId)
    {
        var proveedor = await _context.Proveedores
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == dto.ProveedorId && p.Activo)
            ?? throw new KeyNotFoundException($"No se encontró un proveedor activo con Id {dto.ProveedorId}.");

        var fecha = FechaNegocio.NormalizarUtc(dto.Fecha);

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        var variantes = await _inventario.BloquearVariantesAsync(dto.Detalles.Select(d => d.ProductoVarianteId));

        var compra = new CompraProveedor
        {
            Nombre        = string.IsNullOrWhiteSpace(dto.Nombre) ? null : dto.Nombre.Trim(),
            ProveedorId   = proveedor.Id,
            UsuarioId     = usuarioId,
            Fecha         = fecha,
            NumeroFactura = dto.NumeroFactura,
            Descripcion   = dto.Descripcion,
            CostoEnvio    = dto.CostoEnvio
        };

        foreach (var linea in dto.Detalles)
        {
            var variante = variantes[linea.ProductoVarianteId];

            var detalle = new CompraProveedorDetalle
            {
                ProductoVariante = variante,
                Cantidad         = linea.Cantidad,
                CostoUnitario    = linea.CostoUnitario,
                Subtotal         = linea.Cantidad * linea.CostoUnitario
            };
            compra.Detalles.Add(detalle);

            _inventario.RegistrarEntrada(new EntradaInventario(
                variante, linea.Cantidad, linea.CostoUnitario, MotivosMovimientoInventario.CompraProveedor,
                usuarioId, fecha, $"Compra a {proveedor.Nombre}", compra, detalle));

            // Precio de referencia para el catálogo; el costo real de cada venta sale de la existencia.
            variante.PrecioCompra = linea.CostoUnitario;
        }

        compra.SubtotalProductos = compra.Detalles.Sum(d => d.Subtotal);
        compra.Total             = compra.SubtotalProductos + compra.CostoEnvio;
        _context.ComprasProveedor.Add(compra);

        await _context.SaveChangesAsync();

        // El código depende del Id, que solo existe después de insertar.
        compra.Codigo = $"L-{FechaNegocio.ALocal(fecha).Year}-{compra.Id:D4}";
        await _context.SaveChangesAsync();

        await transaccion.CommitAsync();

        return (await GetByIdAsync(compra.Id))!;
    }

    public async Task<CompraProveedorResponseDto?> AnularAsync(int id, string motivo, int usuarioId)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync();

        var compra = await _context.ComprasProveedor
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compra is null) return null;

        if (compra.Estado == EstadosDocumento.Anulada)
            throw new InvalidOperationException($"El lote {compra.Codigo} ya está anulado.");

        var variantes = await _inventario.BloquearVariantesAsync(
            compra.Detalles.Select(d => d.ProductoVarianteId), exigirActivas: false);

        // Las existencias se leen después del bloqueo para ver el stock real, incluidas ventas recién confirmadas.
        var detalleIds = compra.Detalles.Select(d => d.Id).ToList();
        var existencias = await _context.ExistenciasLote
            .Where(e => e.CompraProveedorDetalleId != null && detalleIds.Contains(e.CompraProveedorDetalleId.Value))
            .ToListAsync();

        foreach (var existencia in existencias)
            _inventario.RetirarExistenciaCompleta(variantes[existencia.ProductoVarianteId], existencia, usuarioId, motivo, compra);

        compra.Estado             = EstadosDocumento.Anulada;
        compra.MotivoAnulacion    = motivo;
        compra.FechaAnulacion     = DateTime.UtcNow;
        compra.UsuarioAnulacionId = usuarioId;

        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return await GetByIdAsync(id);
    }

    private IQueryable<CompraProveedor> QueryConDetalle() =>
        _context.ComprasProveedor
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .Include(c => c.Usuario)
            .Include(c => c.Detalles).ThenInclude(d => d.ProductoVariante).ThenInclude(v => v.Producto)
            .Include(c => c.Detalles).ThenInclude(d => d.Existencia)
            .AsSplitQuery();
}
