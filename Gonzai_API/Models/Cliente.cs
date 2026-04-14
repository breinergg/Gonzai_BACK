using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

[Table("Cliente")]
public class Cliente
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? Telefono { get; set; }
    [MaxLength(150)]
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
    public ICollection<MovimientoCliente> MovimientosCliente { get; set; } = [];
}
