using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

[Table("VentaCliente")]
public class VentaCliente
{
    [Key]
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [MaxLength(150)]
    public string? Descripcion { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoPagado { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal SaldoPendiente { get; set; }
    [Required]
    [MaxLength(20)]
    public string EstadoPago { get; set; } = EstadosPagoVenta.Pendiente;
    [Required]
    [MaxLength(20)]
    public string Estado { get; set; } = EstadosDocumento.Activa;
    [MaxLength(150)]
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public int? UsuarioAnulacionId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }

    [ForeignKey(nameof(ClienteId))]
    public Cliente Cliente { get; set; } = null!;
    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;
    [ForeignKey(nameof(UsuarioAnulacionId))]
    public Usuario? UsuarioAnulacion { get; set; }

    public ICollection<VentaClienteDetalle> Detalles { get; set; } = [];
    public ICollection<PagoCliente> Pagos { get; set; } = [];

    public void RecalcularSaldo()
    {
        SaldoPendiente = Total - MontoPagado;
        EstadoPago     = EstadosPagoVenta.Calcular(Total, MontoPagado);
    }
}
