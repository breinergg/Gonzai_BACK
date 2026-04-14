using System.ComponentModel.DataAnnotations;

namespace Gonzai_API.DTOs.Categoria;

public class CategoriaUpdateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}
