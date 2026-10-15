using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.Producto;

public class ProductoVarianteCreateDto
{
    [MaxLength(20, ErrorMessage = "La talla no puede superar 20 caracteres.")]
    public string? Talla { get; set; }

    [MaxLength(30, ErrorMessage = "El color no puede superar 30 caracteres.")]
    public string? Color { get; set; }

    [MaxLength(50, ErrorMessage = "El SKU no puede superar 50 caracteres.")]
    public string? Sku { get; set; }

    /// <summary>Si no se envía, la variante usa el precio de venta del producto.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor o igual a 0.")]
    public decimal? PrecioVenta { get; set; }

    /// <summary>Unidades que ya existen físicamente (se registran como inventario inicial).</summary>
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int StockInicial { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El costo debe ser mayor o igual a 0.")]
    public decimal? CostoInicial { get; set; }
}

public class ProductoVarianteUpdateDto
{
    [MaxLength(20, ErrorMessage = "La talla no puede superar 20 caracteres.")]
    public string? Talla { get; set; }

    [MaxLength(30, ErrorMessage = "El color no puede superar 30 caracteres.")]
    public string? Color { get; set; }

    [MaxLength(50, ErrorMessage = "El SKU no puede superar 50 caracteres.")]
    public string? Sku { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor o igual a 0.")]
    public decimal? PrecioVenta { get; set; }

    public bool Activo { get; set; } = true;
}

/// <summary>Crea de una vez todas las combinaciones talla × color que aún no existan.</summary>
public class ProductoVarianteMatrizDto
{
    public List<string> Tallas { get; set; } = [];
    public List<string> Colores { get; set; } = [];

    [Range(0, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor o igual a 0.")]
    public decimal? PrecioVenta { get; set; }
}

public class ProductoVarianteResponseDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    /// <summary>Producto + color + talla, listo para mostrar.</summary>
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Talla { get; set; }
    public string? Color { get; set; }
    public string? Sku { get; set; }
    /// <summary>Precio efectivo: el de la variante o, si no tiene, el del producto.</summary>
    public decimal? PrecioVenta { get; set; }
    public bool TienePrecioPropio { get; set; }
    /// <summary>Último costo de compra (referencia).</summary>
    public decimal? PrecioCompra { get; set; }
    public int StockActual { get; set; }
    public bool Activo { get; set; }
}
