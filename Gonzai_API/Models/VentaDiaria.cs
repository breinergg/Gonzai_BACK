using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

[Table("VentaDiaria")]
public class VentaDiaria
{
    [Key]
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }
    [MaxLength(150)]
    public string? Descripcion { get; set; }
    public int? UsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }

    [ForeignKey(nameof(UsuarioId))]
    public Usuario? Usuario { get; set; }
}
