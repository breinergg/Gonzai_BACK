using System.Security.Cryptography;
using Gonzai_API.Data;
using Gonzai_API.DTOs.Negocio;
using Gonzai_API.Models;
using Gonzai_API.Services.Export;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class NegocioService : INegocioService
{
    private const string GmailHost = "smtp.gmail.com";
    private const int GmailPuerto = 587;
    private const string SeguridadPorDefecto = "starttls";

    private readonly AppDbContext _context;
    private readonly IDataProtector _protectorGemini;
    private readonly IDataProtector _protectorSmtp;

    public NegocioService(AppDbContext context, IDataProtectionProvider dataProtection)
    {
        _context = context;
        // El propósito identifica esta clave dentro del llavero: no cifra datos de otra empresa,
        // cada fila sigue siendo independiente porque el texto cifrado depende de su contenido.
        // Cada secreto usa su propio propósito, para que rotar uno no afecte al otro.
        _protectorGemini = dataProtection.CreateProtector("GonzAI.Negocio.GeminiApiKey");
        _protectorSmtp   = dataProtection.CreateProtector("GonzAI.Negocio.SmtpPassword");
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

    public async Task<NegocioResponseDto> ConfigurarClaveGeminiAsync(string apiKey)
    {
        var negocio = await ObtenerOCrearAsync();
        negocio.GeminiApiKeyCifrada = _protectorGemini.Protect(apiKey.Trim());
        negocio.GeminiApiKeyFecha   = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Map(negocio);
    }

    public async Task<NegocioResponseDto> QuitarClaveGeminiAsync()
    {
        var negocio = await ObtenerOCrearAsync();
        negocio.GeminiApiKeyCifrada = null;
        negocio.GeminiApiKeyFecha   = null;

        await _context.SaveChangesAsync();
        return Map(negocio);
    }

    public async Task<bool> TieneClaveGeminiPropiaAsync() =>
        await _context.Negocios
            .AsNoTracking()
            .OrderBy(n => n.Id)
            .Select(n => n.GeminiApiKeyCifrada)
            .FirstOrDefaultAsync() is not null;

    public async Task<string?> ObtenerClaveGeminiDescifradaAsync()
    {
        var cifrada = await _context.Negocios
            .AsNoTracking()
            .OrderBy(n => n.Id)
            .Select(n => n.GeminiApiKeyCifrada)
            .FirstOrDefaultAsync();

        if (cifrada is null) return null;

        try
        {
            return _protectorGemini.Unprotect(cifrada);
        }
        catch (CryptographicException)
        {
            // La clave de cifrado de la plataforma cambió o el valor se dañó: se trata como
            // si la empresa no hubiera configurado ninguna clave, en vez de tumbar el chat.
            return null;
        }
    }

    public async Task<NegocioResponseDto> ConfigurarSmtpAsync(NegocioSmtpDto dto)
    {
        var negocio = await ObtenerOCrearAsync();

        negocio.SmtpHost            = string.IsNullOrWhiteSpace(dto.Host) ? GmailHost : dto.Host.Trim();
        negocio.SmtpPuerto          = dto.Puerto ?? GmailPuerto;
        negocio.SmtpSeguridad       = NormalizarSeguridad(dto.Seguridad);
        negocio.SmtpUsuario         = dto.Usuario.Trim();
        negocio.SmtpPasswordCifrada = _protectorSmtp.Protect(dto.Password);
        negocio.SmtpFecha           = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Map(negocio);
    }

    public async Task<NegocioResponseDto> QuitarSmtpAsync()
    {
        var negocio = await ObtenerOCrearAsync();
        negocio.SmtpHost            = null;
        negocio.SmtpPuerto          = null;
        negocio.SmtpSeguridad       = null;
        negocio.SmtpUsuario         = null;
        negocio.SmtpPasswordCifrada = null;
        negocio.SmtpFecha           = null;

        await _context.SaveChangesAsync();
        return Map(negocio);
    }

    public async Task<SmtpConfigPropia?> ObtenerSmtpPropioAsync()
    {
        var negocio = await _context.Negocios
            .AsNoTracking()
            .OrderBy(n => n.Id)
            .Select(n => new
            {
                n.SmtpHost, n.SmtpPuerto, n.SmtpSeguridad, n.SmtpUsuario, n.SmtpPasswordCifrada
            })
            .FirstOrDefaultAsync();

        if (negocio?.SmtpHost is null || negocio.SmtpUsuario is null || negocio.SmtpPasswordCifrada is null)
            return null;

        try
        {
            var password = _protectorSmtp.Unprotect(negocio.SmtpPasswordCifrada);
            return new SmtpConfigPropia(
                negocio.SmtpHost, negocio.SmtpPuerto ?? GmailPuerto,
                negocio.SmtpSeguridad ?? SeguridadPorDefecto, negocio.SmtpUsuario, password);
        }
        catch (CryptographicException)
        {
            // Igual que con la clave de Gemini: si no se puede descifrar, se trata como si la
            // empresa no hubiera configurado nada, en vez de tumbar el envío de comprobantes.
            return null;
        }
    }

    private static string NormalizarSeguridad(string? valor) => (valor ?? SeguridadPorDefecto).Trim().ToLowerInvariant() switch
    {
        "ssl" => "ssl",
        "none" => "none",
        _ => SeguridadPorDefecto
    };

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
        TieneLogo = negocio.Logo is { Length: > 0 },
        TieneClaveIaPropia = negocio.GeminiApiKeyCifrada is not null,
        ClaveIaFecha = negocio.GeminiApiKeyFecha,
        TieneSmtpPropio = negocio.SmtpHost is not null,
        SmtpUsuario = negocio.SmtpUsuario,
        SmtpFecha = negocio.SmtpFecha
    };

    private static string? Limpio(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
