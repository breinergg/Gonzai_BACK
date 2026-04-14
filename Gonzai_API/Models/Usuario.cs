using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

[Table("Usuario")]
public class Usuario
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;
    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    [Required]
    [MaxLength(20)]
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
    
    public ICollection<VentaDiaria> VentasDiarias { get; set; } = [];
    public ICollection<ChatLog> ChatLogs { get; set; } = [];
}
