namespace Gonzai_API.DTOs.VentaDiaria;

public class VentaMensualResumenDto
{
    public string Mes { get; set; } = string.Empty;
    public string MesAnterior { get; set; } = string.Empty;
    public decimal TotalMesActual { get; set; }
    public decimal TotalMesAnterior { get; set; }
    public decimal PorcentajeCambio { get; set; }
    public bool EsAumento { get; set; }
}
