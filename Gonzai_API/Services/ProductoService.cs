using Gonzai_API.Data;
using Gonzai_API.DTOs.Producto;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class ProductoService : IProductoService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IInventarioService _inventario;

    public ProductoService(AppDbContext context, IMapper mapper, IInventarioService inventario)
    {
        _context    = context;
        _mapper     = mapper;
        _inventario = inventario;
    }

    public async Task<IEnumerable<ProductoResponseDto>> GetAllAsync()
    {
        var productos = await QueryConVariantes(soloActivos: false)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProductoResponseDto>>(productos);
    }

    public async Task<IEnumerable<ProductoResponseDto>> GetAllActivosAsync()
    {
        var productos = await QueryConVariantes(soloActivos: true)
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProductoResponseDto>>(productos);
    }

    public async Task<ProductoResponseDto?> GetByIdAsync(int id)
    {
        var producto = await QueryConVariantes(soloActivos: false).FirstOrDefaultAsync(p => p.Id == id);

        return producto is null ? null : _mapper.Map<ProductoResponseDto>(producto);
    }

    public async Task<ProductoResponseDto> CreateAsync(ProductoCreateDto dto, int usuarioId)
    {
        await ValidarCategoriaAsync(dto.CategoriaId);

        var producto = new Producto
        {
            Nombre      = dto.Nombre.Trim(),
            CategoriaId = dto.CategoriaId,
            Marca       = Normalizar(dto.Marca),
            PrecioVenta = dto.PrecioVenta
        };

        var variantes = dto.Variantes.Count > 0
            ? dto.Variantes
            : [new ProductoVarianteCreateDto { StockInicial = dto.StockInicial, CostoInicial = dto.CostoInicial }];

        ValidarSinDuplicados(variantes.Select(v => (v.Talla, v.Color)), existentes: []);
        await ValidarSkusLibresAsync(variantes.Select(v => v.Sku));

        _context.Productos.Add(producto);

        foreach (var varianteDto in variantes)
            AgregarVariante(producto, varianteDto, usuarioId);

        await _context.SaveChangesAsync();

        return (await GetByIdAsync(producto.Id))!;
    }

    public async Task<ProductoResponseDto?> UpdateAsync(int id, ProductoUpdateDto dto)
    {
        var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);

        if (producto is null) return null;

        await ValidarCategoriaAsync(dto.CategoriaId);

        producto.Nombre      = dto.Nombre.Trim();
        producto.CategoriaId = dto.CategoriaId;
        producto.Marca       = Normalizar(dto.Marca);
        producto.PrecioVenta = dto.PrecioVenta;
        producto.Activo      = dto.Activo;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var producto = await _context.Productos.FindAsync(id);

        if (producto is null) return false;

        // Soft delete: preserva historial de compras, ventas y movimientos de inventario
        producto.Activo = false;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetProductosActivosCountAsync()
    {
        return await _context.Productos
            .AsNoTracking()
            .CountAsync(p => p.Activo);
    }

    public async Task<IEnumerable<ProductoResponseDto>> SearchAsync(string? nombre, bool soloActivos = true, int limit = 20)
    {
        var query = QueryConVariantes(soloActivos);

        if (soloActivos)
            query = query.Where(p => p.Activo);

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            var patron = $"%{nombre.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Nombre, patron)
                                     || (p.Marca != null && EF.Functions.ILike(p.Marca, patron))
                                     || p.Variantes.Any(v => v.Sku != null && EF.Functions.ILike(v.Sku, patron)));
        }

        var productos = await query
            .OrderBy(p => p.Nombre)
            .Take(limit)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProductoResponseDto>>(productos);
    }

    public async Task<ProductoVarianteResponseDto?> GetVarianteByIdAsync(int varianteId)
    {
        var variante = await _context.ProductoVariantes
            .AsNoTracking()
            .Include(v => v.Producto)
            .FirstOrDefaultAsync(v => v.Id == varianteId);

        return variante is null ? null : _mapper.Map<ProductoVarianteResponseDto>(variante);
    }

    public async Task<ProductoVarianteResponseDto> CreateVarianteAsync(int productoId, ProductoVarianteCreateDto dto, int usuarioId)
    {
        var producto = await CargarProductoConVariantesAsync(productoId);

        ValidarSinDuplicados([(dto.Talla, dto.Color)], producto.Variantes);
        await ValidarSkusLibresAsync([dto.Sku]);

        if (Normalizar(dto.Talla) is not null || Normalizar(dto.Color) is not null)
            await QuitarVariantePorDefectoSinUsoAsync(producto);

        var variante = AgregarVariante(producto, dto, usuarioId);
        await _context.SaveChangesAsync();

        return (await GetVarianteByIdAsync(variante.Id))!;
    }

    public async Task<IEnumerable<ProductoVarianteResponseDto>> CreateMatrizAsync(int productoId, ProductoVarianteMatrizDto dto, int usuarioId)
    {
        var tallas  = LimpiarLista(dto.Tallas);
        var colores = LimpiarLista(dto.Colores);

        if (tallas.Count == 0 && colores.Count == 0)
            throw new InvalidOperationException("Debe indicar al menos una talla o un color.");

        var producto = await CargarProductoConVariantesAsync(productoId);
        await QuitarVariantePorDefectoSinUsoAsync(producto);

        var combinaciones =
            from color in colores.Count > 0 ? colores : [null]
            from talla in tallas.Count > 0 ? tallas : [null]
            select (Talla: talla, Color: color);

        var existentes = producto.Variantes.Select(v => Clave(v.Talla, v.Color)).ToHashSet();
        var nuevas = new List<ProductoVariante>();

        foreach (var (talla, color) in combinaciones)
        {
            if (!existentes.Add(Clave(talla, color))) continue;

            nuevas.Add(AgregarVariante(producto,
                new ProductoVarianteCreateDto { Talla = talla, Color = color, PrecioVenta = dto.PrecioVenta }, usuarioId));
        }

        await _context.SaveChangesAsync();

        var ids = nuevas.Select(v => v.Id).ToList();
        var variantes = await _context.ProductoVariantes
            .AsNoTracking()
            .Include(v => v.Producto)
            .Where(v => ids.Contains(v.Id))
            .OrderBy(v => v.Id)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProductoVarianteResponseDto>>(variantes);
    }

    public async Task<ProductoVarianteResponseDto?> UpdateVarianteAsync(int varianteId, ProductoVarianteUpdateDto dto)
    {
        var variante = await _context.ProductoVariantes.FirstOrDefaultAsync(v => v.Id == varianteId);

        if (variante is null) return null;

        var hermanas = await _context.ProductoVariantes
            .Where(v => v.ProductoId == variante.ProductoId && v.Id != varianteId)
            .ToListAsync();

        ValidarSinDuplicados([(dto.Talla, dto.Color)], hermanas);
        await ValidarSkusLibresAsync([dto.Sku], excluirVarianteId: varianteId);

        variante.Talla       = NormalizarTalla(dto.Talla);
        variante.Color       = NormalizarColor(dto.Color);
        variante.Sku         = NormalizarSku(dto.Sku);
        variante.PrecioVenta = dto.PrecioVenta;
        variante.Activo      = dto.Activo;

        await _context.SaveChangesAsync();

        return await GetVarianteByIdAsync(varianteId);
    }

    public async Task<bool> DeleteVarianteAsync(int varianteId)
    {
        var variante = await _context.ProductoVariantes.FindAsync(varianteId);

        if (variante is null) return false;

        variante.Activo = false;
        await _context.SaveChangesAsync();

        return true;
    }

    private ProductoVariante AgregarVariante(Producto producto, ProductoVarianteCreateDto dto, int usuarioId)
    {
        var variante = new ProductoVariante
        {
            Producto     = producto,
            Talla        = NormalizarTalla(dto.Talla),
            Color        = NormalizarColor(dto.Color),
            Sku          = NormalizarSku(dto.Sku),
            PrecioVenta  = dto.PrecioVenta,
            PrecioCompra = dto.CostoInicial
        };

        producto.Variantes.Add(variante);

        // El stock solo existe dentro de existencias: el stock inicial se registra como una propia.
        if (dto.StockInicial > 0)
        {
            _inventario.RegistrarEntrada(new EntradaInventario(
                variante, dto.StockInicial, dto.CostoInicial ?? 0m, MotivosMovimientoInventario.InventarioInicial,
                usuarioId, DateTime.UtcNow, "Inventario inicial"));
        }

        return variante;
    }

    /// <summary>
    /// La variante sin talla ni color solo tiene sentido mientras el producto no tenga variantes reales.
    /// Si nunca tuvo inventario se elimina; si tuvo, se conserva por su historial.
    /// </summary>
    private async Task QuitarVariantePorDefectoSinUsoAsync(Producto producto)
    {
        var porDefecto = producto.Variantes.FirstOrDefault(v => v.Talla is null && v.Color is null);
        if (porDefecto is null || porDefecto.StockActual > 0) return;

        if (await _context.ExistenciasLote.AnyAsync(e => e.ProductoVarianteId == porDefecto.Id)) return;

        producto.Variantes.Remove(porDefecto);
        _context.ProductoVariantes.Remove(porDefecto);
    }

    private async Task<Producto> CargarProductoConVariantesAsync(int productoId) =>
        await _context.Productos
            .Include(p => p.Variantes)
            .FirstOrDefaultAsync(p => p.Id == productoId)
        ?? throw new KeyNotFoundException($"No se encontró el producto con Id {productoId}.");

    private IQueryable<Producto> QueryConVariantes(bool soloActivos) =>
        soloActivos
            ? _context.Productos.AsNoTracking().Include(p => p.Categoria)
                .Include(p => p.Variantes.Where(v => v.Activo).OrderBy(v => v.Id))
            : _context.Productos.AsNoTracking().Include(p => p.Categoria)
                .Include(p => p.Variantes.OrderBy(v => v.Id));

    private async Task ValidarCategoriaAsync(int? categoriaId)
    {
        if (categoriaId.HasValue && !await _context.Categorias.AnyAsync(c => c.Id == categoriaId.Value))
            throw new KeyNotFoundException($"No se encontró la categoría con Id {categoriaId}.");
    }

    private static void ValidarSinDuplicados(
        IEnumerable<(string? Talla, string? Color)> nuevas, IEnumerable<ProductoVariante> existentes)
    {
        var claves = existentes.Select(v => Clave(v.Talla, v.Color)).ToHashSet();

        foreach (var (talla, color) in nuevas)
        {
            if (!claves.Add(Clave(talla, color)))
            {
                var nombre = ProductoVariante.Componer(null, NormalizarColor(color), NormalizarTalla(talla));
                throw new InvalidOperationException(
                    $"La variante '{(nombre.Length > 0 ? nombre : "sin talla ni color")}' ya existe en este producto.");
            }
        }
    }

    private async Task ValidarSkusLibresAsync(IEnumerable<string?> skus, int? excluirVarianteId = null)
    {
        var lista = skus.Select(NormalizarSku).OfType<string>().ToList();
        if (lista.Count == 0) return;

        var repetido = lista.GroupBy(s => s).FirstOrDefault(g => g.Count() > 1);
        if (repetido is not null)
            throw new InvalidOperationException($"El SKU '{repetido.Key}' está repetido.");

        var enUso = await _context.ProductoVariantes
            .Where(v => v.Sku != null && lista.Contains(v.Sku) && v.Id != excluirVarianteId)
            .Select(v => v.Sku)
            .FirstOrDefaultAsync();

        if (enUso is not null)
            throw new InvalidOperationException($"El SKU '{enUso}' ya está asignado a otra variante.");
    }

    private static string Clave(string? talla, string? color) =>
        $"{NormalizarTalla(talla)}|{NormalizarColor(color)}";

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string? NormalizarTalla(string? talla) => Normalizar(talla)?.ToUpperInvariant();

    private static string? NormalizarSku(string? sku) => Normalizar(sku)?.ToUpperInvariant();

    /// <summary>"negro", "NEGRO" y "Negro" son el mismo color.</summary>
    private static string? NormalizarColor(string? color) =>
        Normalizar(color) is { } c ? Cultura.TextInfo.ToTitleCase(c.ToLower(Cultura)) : null;

    private static readonly System.Globalization.CultureInfo Cultura = new("es-CO");

    private static List<string?> LimpiarLista(IEnumerable<string> valores) =>
        valores.Select(Normalizar).OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string?>()
            .ToList();
}
