namespace Gonzai_API.DTOs.VentaDiaria;

public class VentaDiariaResponseDto
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal Total { get; set; }
    public string? Descripcion { get; set; }
    public int? UsuarioId { get; set; }
    public string? UsuarioNombre { get; set; }
    public DateTime FechaCreacion { get; set; }
}
