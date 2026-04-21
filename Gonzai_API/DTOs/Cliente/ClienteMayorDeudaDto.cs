namespace Gonzai_API.DTOs.Cliente;

public class ClienteMayorDeudaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public decimal TotalDeuda { get; set; }
}
