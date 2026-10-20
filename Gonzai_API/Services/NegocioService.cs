using Gonzai_API.Data;
using Gonzai_API.DTOs.Negocio;
using Gonzai_API.Models;
using Gonzai_API.Services.Export;
using Gonzai_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class NegocioService : INegocioService
{
    private readonly AppDbContext _context;

    public NegocioService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<NegocioResponseDto> GetAsync()
    {
        var negocio = await ObtenerOCrearAsync();
        return Map(negocio);
    }

    public async Task<NegocioResponseDto> UpdateAsync(NegocioUpdateDto dto)
    {
        var negocio = await ObtenerOCrearAsync();
        negocio.NombreComercial = dto.NombreComercial.Trim();
        negocio.Telefono = Limpio(dto.Telefono);
        negocio.Ciudad = Limpio(dto.Ciudad);
        negocio.CorreoRespuesta = Limpio(dto.CorreoRespuesta);
        negocio.Mensaje = string.IsNullOrWhiteSpace(dto.Mensaje) ? "Gracias por tu compra." : dto.Mensaje.Trim();
        negocio.FormatoPredeterminado = FormatosComprobante.Normalizar(
            string.IsNullOrWhiteSpace(dto.FormatoPredeterminado) ? negocio.FormatoPredeterminado : dto.FormatoPredeterminado);

        await _context.SaveChangesAsync();
        return Map(negocio);
    }

    public async Task<NegocioResponseDto> UpdateLogoAsync(byte[] png)
    {
        LogoPng.Validar(png);
        var negocio = await ObtenerOCrearAsync();
        negocio.Logo = png;
        await _context.SaveChangesAsync();
        return Map(negocio);
    }

    public async Task<NegocioResponseDto> QuitarLogoAsync()
    {
        var negocio = await ObtenerOCrearAsync();
        negocio.Logo = null;
        await _context.SaveChangesAsync();
        return Map(negocio);
    }

    public async Task<byte[]?> GetLogoAsync()
    {
        var negocio = await _context.Negocios.AsNoTracking().OrderBy(n => n.Id).FirstOrDefaultAsync();
        return negocio?.Logo;
    }

    private async Task<Negocio> ObtenerOCrearAsync()
    {
        var negocio = await _context.Negocios.OrderBy(n => n.Id).FirstOrDefaultAsync();
        if (negocio is not null) return negocio;

        negocio = new Negocio();
        _context.Negocios.Add(negocio);
        await _context.SaveChangesAsync();
        return negocio;
    }

    private static NegocioResponseDto Map(Negocio negocio) => new()
    {
        Id = negocio.Id,
        NombreComercial = negocio.NombreComercial,
        Telefono = negocio.Telefono,
        Ciudad = negocio.Ciudad,
        CorreoRespuesta = negocio.CorreoRespuesta,
        Mensaje = negocio.Mensaje,
        FormatoPredeterminado = negocio.FormatoPredeterminado,
        TieneLogo = negocio.Logo is { Length: > 0 }
    };

    private static string? Limpio(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
