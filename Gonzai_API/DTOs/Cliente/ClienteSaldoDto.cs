namespace Gonzai_API.DTOs.Cliente;

public class ClienteSaldoDto
{
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal TotalComprado { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal Saldo { get; set; }
    public bool EnPazYSalvo => Saldo <= 0;
    public List<VentaPendienteDto> VentasPendientes { get; set; } = [];
}

public class VentaPendienteDto
{
    public int VentaClienteId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string EstadoPago { get; set; } = string.Empty;
}
