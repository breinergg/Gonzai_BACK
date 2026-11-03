using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.Plataforma;

public class EmpresaCreateDto
{
    [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Identificador corto y permanente (por ejemplo "luxury"). Da nombre a su base de datos.
    /// Si no se envía, se deriva del nombre.
    /// </summary>
    [MaxLength(40, ErrorMessage = "El identificador no puede superar 40 caracteres.")]
    [RegularExpression("^[a-zA-Z0-9_-]*$", ErrorMessage = "El identificador solo admite letras, números, guion y guion bajo.")]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "El correo del administrador es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo del administrador no es válido.")]
    [MaxLength(100, ErrorMessage = "El correo no puede superar 100 caracteres.")]
    public string AdminEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del administrador es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string AdminNombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña del administrador es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>Consultas a la IA por día. Con 0, la empresa no tiene asistente.</summary>
    [Range(0, 100_000, ErrorMessage = "El límite de chats diarios no es válido.")]
    public int LimiteChatsDiarios { get; set; } = 200;
}

public class EmpresaResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string BaseDatos { get; set; } = string.Empty;
    public bool Activa { get; set; }
    public int LimiteChatsDiarios { get; set; }
    public DateTime FechaCreacion { get; set; }
}
