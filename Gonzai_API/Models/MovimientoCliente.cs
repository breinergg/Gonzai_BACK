using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

[Table("MovimientoCliente")]
public class MovimientoCliente
{
    [Key]
    public int Id { get; set; }
    public int ClienteId { get; set; }
    [Required]
    [MaxLength(20)]
    public string TipoMovimiento { get; set; } = string.Empty;
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Valor { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [MaxLength(150)]
    public string? Descripcion { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
    [ForeignKey(nameof(ClienteId))]
    public Cliente Cliente { get; set; } = null!;
}
