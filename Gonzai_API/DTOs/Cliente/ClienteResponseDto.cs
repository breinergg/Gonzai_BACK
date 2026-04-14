namespace Gonzai_API.DTOs.Cliente;

public class ClienteResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
