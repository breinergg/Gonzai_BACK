using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models.Plataforma;

/// <summary>
/// Dice a qué empresa pertenece un correo. Permite que el login siga pidiendo solo
/// correo y contraseña: con el correo se sabe a qué base de datos hay que ir a validar.
/// La contraseña nunca se guarda aquí, solo en la base de la empresa.
/// </summary>
[Table("UsuarioDirectorio")]
public class UsuarioDirectorio
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    public int EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
