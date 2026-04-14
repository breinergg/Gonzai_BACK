using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models;

[Table("PreguntasNoReconocidas")]
public class PreguntaNoReconocida
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Pregunta { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
