using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gonzai_API.Models.Plataforma;

/// <summary>
/// Una empresa cliente de GonzAI. Vive en la base de catálogo (no en la de la empresa) y
/// dice en qué base de datos están sus clientes, ventas e inventario.
/// </summary>
[Table("Empresa")]
public class Empresa
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Identificador corto y estable (solo letras, números y guion bajo).</summary>
    [Required]
    [MaxLength(40)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Nombre de la base de datos PostgreSQL de esta empresa.</summary>
    [Required]
    [MaxLength(63)]
    public string BaseDatos { get; set; } = string.Empty;

    /// <summary>Una empresa inactiva no puede iniciar sesión (por ejemplo, si dejó de pagar).</summary>
    public bool Activa { get; set; } = true;

    /// <summary>
    /// Tope de consultas a la IA por día. Evita que el consumo de Gemini, que se cobra a la
    /// plataforma, se dispare. Con 0 el asistente queda deshabilitado para esta empresa.
    /// </summary>
    public int LimiteChatsDiarios { get; set; } = 200;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
}
