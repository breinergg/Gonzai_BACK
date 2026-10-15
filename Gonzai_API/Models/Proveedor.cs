using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

[Table("Proveedor")]
public class Proveedor
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;
    [MaxLength(30)]
    public string? Documento { get; set; }
    [MaxLength(20)]
    public string? Telefono { get; set; }
    [MaxLength(150)]
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }

    public ICollection<CompraProveedor> Compras { get; set; } = [];
}
