namespace Gonzai_API.DTOs.Categoria;

public class CategoriaResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}
