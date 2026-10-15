using Gonzai_API.Models;

namespace Gonzai_API.Services.Interfaces;

public sealed record EntradaInventario(
    ProductoVariante Variante,
    int Cantidad,
    decimal CostoUnitario,
    string Motivo,
    int UsuarioId,
    DateTime Fecha,
    string? Descripcion,
    CompraProveedor? Compra = null,
    CompraProveedorDetalle? CompraDetalle = null);

/// <summary>
/// Salida de inventario. Con <paramref name="CompraProveedorId"/> se descuenta solo de ese lote (compra);
/// con <paramref name="ExistenciaLoteId"/>, de una existencia puntual; sin ninguno, FIFO.
/// </summary>
public sealed record SalidaInventario(
    ProductoVariante Variante,
    int Cantidad,
    string Motivo,
    int UsuarioId,
    DateTime Fecha,
    string? Descripcion,
    int? CompraProveedorId = null,
    int? ExistenciaLoteId = null,
    VentaCliente? Venta = null);

public sealed record ConsumoExistencia(ExistenciaLote Existencia, int Cantidad);

/// <summary>
/// Motor de inventario por lotes. No llama a SaveChanges: el servicio que lo usa
/// controla la transacción para que el documento y el inventario se guarden juntos.
/// </summary>
public interface IInventarioService
{
    /// <summary>
    /// Bloquea las filas de las variantes (SELECT ... FOR UPDATE) hasta que termine la transacción
    /// activa, para que dos operaciones simultáneas no vendan el mismo stock. Carga su producto.
    /// </summary>
    Task<Dictionary<int, ProductoVariante>> BloquearVariantesAsync(
        IEnumerable<int> varianteIds, bool exigirActivas = true, CancellationToken ct = default);

    /// <summary>Crea una existencia nueva y su movimiento de entrada.</summary>
    ExistenciaLote RegistrarEntrada(EntradaInventario entrada);

    Task<List<ConsumoExistencia>> RegistrarSalidaAsync(SalidaInventario salida, CancellationToken ct = default);

    /// <summary>Devuelve unidades a la existencia de la que salieron (anulación de venta).</summary>
    void Reintegrar(ProductoVariante variante, ExistenciaLote existencia, int cantidad, int usuarioId,
        string? descripcion, VentaCliente venta);

    /// <summary>Retira toda la existencia (anulación de compra). Falla si ya se usó.</summary>
    void RetirarExistenciaCompleta(ProductoVariante variante, ExistenciaLote existencia, int usuarioId,
        string? descripcion, CompraProveedor compra);
}
