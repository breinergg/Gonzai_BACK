using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

/// <summary>
/// Compra a un proveedor. Para el negocio es un "lote": un paquete con varias prendas
/// distintas, identificado por <see cref="Codigo"/> (L-2026-0015).
/// </summary>
[Table("CompraProveedor")]
public class CompraProveedor
{
    [Key]
    public int Id { get; set; }
    /// <summary>Se asigna al guardar, a partir del año local de la compra y su Id.</summary>
    [MaxLength(20)]
    public string? Codigo { get; set; }
    [MaxLength(100)]
    public string? Nombre { get; set; }
    public int ProveedorId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [MaxLength(50)]
    public string? NumeroFactura { get; set; }
    [MaxLength(150)]
    public string? Descripcion { get; set; }
    /// <summary>Suma de las prendas (cantidad × costo unitario).</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal SubtotalProductos { get; set; }
    /// <summary>
    /// Gasto de envío del lote. No se reparte en el costo de las prendas:
    /// se descuenta en la rentabilidad del lote.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostoEnvio { get; set; }
    /// <summary>SubtotalProductos + CostoEnvio.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }
    [Required]
    [MaxLength(20)]
    public string Estado { get; set; } = EstadosDocumento.Activa;
    [MaxLength(150)]
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public int? UsuarioAnulacionId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }

    [ForeignKey(nameof(ProveedorId))]
    public Proveedor Proveedor { get; set; } = null!;
    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;
    [ForeignKey(nameof(UsuarioAnulacionId))]
    public Usuario? UsuarioAnulacion { get; set; }

    public ICollection<CompraProveedorDetalle> Detalles { get; set; } = [];
}
