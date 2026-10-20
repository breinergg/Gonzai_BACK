namespace Gonzai_API.DTOs.MovimientoInventario;

public class MovimientoInventarioResponseDto
{
    public int Id { get; set; }
    public int ProductoVarianteId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string VarianteNombre { get; set; } = string.Empty;
    public int ExistenciaId { get; set; }
    /// <summary>Lote (compra) al que pertenecen las unidades movidas, si vienen de una compra.</summary>
    public string? LoteCodigo { get; set; }
    /// <summary>Origen de la existencia: compra, inventario inicial o ajuste.</summary>
    public string Origen { get; set; } = string.Empty;
    public string TipoMovimiento { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    /// <summary>Stock de la variante después del movimiento.</summary>
    public int StockResultante { get; set; }
    public int? CompraProveedorId { get; set; }
    public int? VentaClienteId { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? Descripcion { get; set; }
}
