namespace Gonzai_API.DTOs.Lote;

/// <summary>
/// Un lote del negocio = una compra a proveedor con todas sus prendas.
/// Los montos de venta se calculan sobre ventas activas (anuladas no cuentan).
/// </summary>
public class LoteResumenDto
{
    public int CompraProveedorId { get; set; }
    public string? Codigo { get; set; }
    public string? Nombre { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;

    public int UnidadesCompradas { get; set; }
    public int UnidadesVendidas { get; set; }
    public int UnidadesDisponibles { get; set; }
    /// <summary>Unidades que salieron por ajustes manuales (pérdidas, daños, conteos).</summary>
    public int UnidadesAjustadas { get; set; }

    /// <summary>Lo pagado por las prendas.</summary>
    public decimal SubtotalProductos { get; set; }
    public decimal CostoEnvio { get; set; }
    /// <summary>Prendas + envío.</summary>
    public decimal InversionTotal { get; set; }

    /// <summary>Valor vendido (precio de venta × unidades). Incluye lo que los clientes aún deben.</summary>
    public decimal Ingresos { get; set; }
    /// <summary>Costo de las prendas que ya se vendieron.</summary>
    public decimal CostoVendido { get; set; }
    /// <summary>Ingresos − costo de lo vendido.</summary>
    public decimal GananciaBruta { get; set; }
    /// <summary>Ganancia bruta − envío.</summary>
    public decimal GananciaNeta { get; set; }
    /// <summary>Costo de las prendas que siguen en inventario.</summary>
    public decimal ValorInventarioDisponible { get; set; }

    public decimal PorcentajeVendido { get; set; }
    /// <summary>Ingresos / inversión total × 100. 100 % o más = el lote ya se pagó solo.</summary>
    public decimal PorcentajeRecuperado { get; set; }
    public bool InversionRecuperada { get; set; }
}

public class LoteLineaDto
{
    public int CompraProveedorDetalleId { get; set; }
    public int? ExistenciaId { get; set; }
    public int ProductoVarianteId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string VarianteNombre { get; set; } = string.Empty;
    public string? Talla { get; set; }
    public string? Color { get; set; }
    public decimal CostoUnitario { get; set; }
    public int Comprado { get; set; }
    public int Vendido { get; set; }
    public int Disponible { get; set; }
    public int Ajustado { get; set; }
    public decimal Ingresos { get; set; }
    public decimal CostoVendido { get; set; }
    public decimal GananciaBruta { get; set; }
}

public class LoteDetalleDto : LoteResumenDto
{
    public string? NumeroFactura { get; set; }
    public string? Descripcion { get; set; }
    public List<LoteLineaDto> Lineas { get; set; } = [];
}

/// <summary>Stock disponible de una variante agrupado por lote, para elegir de dónde vender.</summary>
public class ExistenciaDisponibleDto
{
    public int ExistenciaId { get; set; }
    public int ProductoVarianteId { get; set; }
    public string VarianteNombre { get; set; } = string.Empty;
    /// <summary>Null si la existencia no viene de una compra (inventario inicial o ajuste).</summary>
    public int? CompraProveedorId { get; set; }
    public string? LoteCodigo { get; set; }
    public string? LoteNombre { get; set; }
    public string? ProveedorNombre { get; set; }
    public string Origen { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }
    public int CantidadDisponible { get; set; }
    public decimal CostoUnitario { get; set; }
}
