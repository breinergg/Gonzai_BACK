using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

[Table("ChatLog")]
public class ChatLog
{
    [Key]
    public int Id { get; set; }
    public int? UsuarioId { get; set; }
    [Required]
    public string Pregunta { get; set; } = string.Empty;
    public string? Respuesta { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UsuarioId))]
    public Usuario? Usuario { get; set; }
}
