using Gonzai_API.DTOs.Negocio;

namespace Gonzai_API.Services.Interfaces;

public interface INegocioService
{
    Task<NegocioResponseDto> GetAsync();
    Task<NegocioResponseDto> UpdateAsync(NegocioUpdateDto dto);
    Task<NegocioResponseDto> UpdateLogoAsync(byte[] png);
    Task<NegocioResponseDto> QuitarLogoAsync();
    Task<byte[]?> GetLogoAsync();

    /// <summary>Cifra y guarda la clave propia de Gemini de la empresa.</summary>
    Task<NegocioResponseDto> ConfigurarClaveGeminiAsync(string apiKey);

    /// <summary>Vuelve a usar la clave compartida de la plataforma (sujeta a su cupo diario).</summary>
    Task<NegocioResponseDto> QuitarClaveGeminiAsync();

    /// <summary>Comprobación barata (no descifra nada) para saber si aplica el cupo compartido.</summary>
    Task<bool> TieneClaveGeminiPropiaAsync();

    /// <summary>
    /// Clave de Gemini de la empresa ya descifrada, o null si no configuró una o si no se pudo
    /// descifrar (por ejemplo, tras rotar la clave de cifrado de la plataforma).
    /// </summary>
    Task<string?> ObtenerClaveGeminiDescifradaAsync();

    /// <summary>Cifra y guarda el correo propio de la empresa para enviar comprobantes.</summary>
    Task<NegocioResponseDto> ConfigurarSmtpAsync(NegocioSmtpDto dto);

    /// <summary>Vuelve a usar el correo compartido de la plataforma.</summary>
    Task<NegocioResponseDto> QuitarSmtpAsync();

    /// <summary>
    /// Configuración SMTP propia ya descifrada, o null si la empresa no configuró una (o no se
    /// pudo descifrar). La usa <see cref="Email.SmtpEmailSender"/> antes de caer a la compartida.
    /// </summary>
    Task<SmtpConfigPropia?> ObtenerSmtpPropioAsync();
}

/// <summary>Configuración SMTP de una empresa, ya descifrada. Solo para uso interno del envío de correo.</summary>
public record SmtpConfigPropia(string Host, int Puerto, string Seguridad, string Usuario, string Password);
