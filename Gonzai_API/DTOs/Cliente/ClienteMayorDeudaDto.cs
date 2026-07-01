namespace Gonzai_API.DTOs.Cliente;

public class ClienteMayorDeudaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }

    /// <summary>
    /// Saldo pendiente real: suma de deudas menos suma de abonos.
    /// </summary>
    public decimal TotalDeuda { get; set; }
}
