using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

[Table("PagoCliente")]
public class PagoCliente
{
    [Key]
    public int Id { get; set; }
    public int VentaClienteId { get; set; }
    public int ClienteId { get; set; }
    public int UsuarioId { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }
    [Required]
    [MaxLength(20)]
    public string MetodoPago { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [MaxLength(150)]
    public string? Descripcion { get; set; }
    [Required]
    [MaxLength(20)]
    public string Estado { get; set; } = EstadosPago.Activo;
    [MaxLength(150)]
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public int? UsuarioAnulacionId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }

    [ForeignKey(nameof(VentaClienteId))]
    public VentaCliente VentaCliente { get; set; } = null!;
    [ForeignKey(nameof(ClienteId))]
    public Cliente Cliente { get; set; } = null!;
    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;
    [ForeignKey(nameof(UsuarioAnulacionId))]
    public Usuario? UsuarioAnulacion { get; set; }
}
