using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.AI;

public class MaxListLengthAttribute : ValidationAttribute
{
    private readonly int _maxLength;
    public MaxListLengthAttribute(int maxLength) => _maxLength = maxLength;

    protected override ValidationResult? IsValid(object? value, ValidationContext context) =>
        value is System.Collections.ICollection { Count: var count } && count > _maxLength
            ? new ValidationResult($"El historial no puede tener más de {_maxLength} mensajes.")
            : ValidationResult.Success;
}

public class ChatMessageDto
{
    [Required]
    [MaxLength(10)]
    public string Role { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Text { get; set; } = string.Empty;
}

public class ChatRequestDto
{
    [Required(ErrorMessage = "La pregunta es obligatoria.")]
    [MaxLength(2000, ErrorMessage = "La pregunta no puede superar los 2000 caracteres.")]
    public string Pregunta { get; set; } = string.Empty;

    /// <summary>
    /// Historial opcional de la conversación. Si no se envía, se carga desde ChatLogs
    /// del usuario autenticado.
    /// </summary>
    [MaxListLength(20)]
    public List<ChatMessageDto>? Historial { get; set; }

    /// <summary>
    /// Si es true y no se envía Historial, carga los últimos mensajes del usuario desde BD.
    /// </summary>
    public bool UsarHistorialDeBd { get; set; } = true;
}
