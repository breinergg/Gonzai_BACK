namespace Gonzai_API.DTOs.Cliente;

public class ClienteMayorDeudaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }

    /// <summary>
    /// Suma de los saldos pendientes de las ventas activas del cliente.
    /// </summary>
    public decimal TotalDeuda { get; set; }
}
