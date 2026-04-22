namespace Gonzai_API.DTOs.Cliente;

public class ClienteSaldoDto
{
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal TotalDeuda { get; set; }
    public decimal TotalAbonos { get; set; }
    public decimal Saldo { get; set; }
    public bool EnPazYSalvo => Saldo <= 0;
}
