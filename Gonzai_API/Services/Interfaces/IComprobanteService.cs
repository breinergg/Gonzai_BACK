using Gonzai_API.DTOs.Comprobante;

namespace Gonzai_API.Services.Interfaces;

public interface IComprobanteService
{
    Task<(byte[] Pdf, string NombreArchivo)> VentaPdfAsync(int ventaId, string? formato);
    Task<(byte[] Pdf, string NombreArchivo)> AbonoPdfAsync(int pagoId, string? formato);
    Task<CompartirComprobanteDto> CompartirVentaAsync(int ventaId, string? formato, bool regenerar, string baseUrl);
    Task<CompartirComprobanteDto> CompartirAbonoAsync(int pagoId, string? formato, bool regenerar, string baseUrl);
    Task EnviarVentaAsync(int ventaId, string correo, string? formato, string baseUrl);
    Task EnviarAbonoAsync(int pagoId, string correo, string? formato, string baseUrl);
    Task<(byte[] Pdf, string NombreArchivo)?> PorTokenAsync(string token);
}
