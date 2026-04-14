namespace Gonzai_API.DTOs.MovimientoInventario;

public class MovimientoInventarioResponseDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string TipoMovimiento { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
    public string? Descripcion { get; set; }
}
