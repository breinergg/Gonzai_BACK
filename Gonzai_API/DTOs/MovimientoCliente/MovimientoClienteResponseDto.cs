namespace Gonzai_API.DTOs.MovimientoCliente;

public class MovimientoClienteResponseDto
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string TipoMovimiento { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime Fecha { get; set; }
    public string? Descripcion { get; set; }
}
