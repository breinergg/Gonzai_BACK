using Gonzai_API.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Gonzai_API.Services.Email;

public interface IEmailSender
{
    Task EnviarAsync(
        string para,
        string asunto,
        string html,
        string nombreRemitente,
        string? responderA,
        string nombreAdjunto,
        byte[] pdf);

    /// <summary>Correo corto sin adjunto, para que un Admin confirme que su configuración de correo funciona.</summary>
    Task EnviarPruebaAsync(string para, string nombreRemitente);
}

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly INegocioService _negocio;

    public SmtpEmailSender(IConfiguration config, INegocioService negocio)
    {
        _config  = config;
        _negocio = negocio;
    }

    public async Task EnviarAsync(
        string para,
        string asunto,
        string html,
        string nombreRemitente,
        string? responderA,
        string nombreAdjunto,
        byte[] pdf)
    {
        var smtp = await ResolverConfigAsync();

        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(nombreRemitente, smtp.From));
        mensaje.To.Add(MailboxAddress.Parse(para));
        if (!string.IsNullOrWhiteSpace(responderA))
            mensaje.ReplyTo.Add(new MailboxAddress(nombreRemitente, responderA));
        mensaje.Subject = asunto;

        var cuerpo = new BodyBuilder { HtmlBody = html };
        cuerpo.Attachments.Add(nombreAdjunto, pdf, new ContentType("application", "pdf"));
        mensaje.Body = cuerpo.ToMessageBody();

        var seguridad = smtp.Seguridad switch
        {
            "ssl" => SecureSocketOptions.SslOnConnect,
            "none" => SecureSocketOptions.None,
            _ => SecureSocketOptions.StartTls
        };

        using var cliente = new SmtpClient();
        if (!_config.GetValue("Smtp:CheckCertificateRevocation", true))
            cliente.CheckCertificateRevocation = false;
        await cliente.ConnectAsync(smtp.Host, smtp.Puerto, seguridad);
        await cliente.AuthenticateAsync(smtp.Usuario, smtp.Password);
        await cliente.SendAsync(mensaje);
        await cliente.DisconnectAsync(true);
    }

    public async Task EnviarPruebaAsync(string para, string nombreRemitente)
    {
        var smtp = await ResolverConfigAsync();

        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(nombreRemitente, smtp.From));
        mensaje.To.Add(MailboxAddress.Parse(para));
        mensaje.Subject = "Prueba de configuración de correo — GonzAI";
        mensaje.Body = new TextPart("plain")
        {
            Text = $"Este correo confirma que {nombreRemitente} ya puede enviar comprobantes " +
                   "por correo desde GonzAI. Si lo recibiste, todo está listo."
        };

        var seguridad = smtp.Seguridad switch
        {
            "ssl" => SecureSocketOptions.SslOnConnect,
            "none" => SecureSocketOptions.None,
            _ => SecureSocketOptions.StartTls
        };

        using var cliente = new SmtpClient();
        if (!_config.GetValue("Smtp:CheckCertificateRevocation", true))
            cliente.CheckCertificateRevocation = false;
        await cliente.ConnectAsync(smtp.Host, smtp.Puerto, seguridad);
        await cliente.AuthenticateAsync(smtp.Usuario, smtp.Password);
        await cliente.SendAsync(mensaje);
        await cliente.DisconnectAsync(true);
    }

    /// <summary>
    /// El correo propio de la empresa tiene prioridad: sus envíos cuentan para su propio límite
    /// diario, no el de la plataforma. Si no configuró uno, se usa la cuenta compartida.
    /// </summary>
    private async Task<(string Host, int Puerto, string Seguridad, string Usuario, string Password, string From)> ResolverConfigAsync()
    {
        var propio = await _negocio.ObtenerSmtpPropioAsync();
        if (propio is not null)
            return (propio.Host, propio.Puerto, propio.Seguridad, propio.Usuario, propio.Password, propio.Usuario);

        var host = _config["Smtp:Host"];
        var from = _config["Smtp:From"];
        var usuario = _config["Smtp:User"];
        var password = _config["Smtp:Password"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from)
            || string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                "El envío de correo no está configurado para tu empresa. " +
                "Un administrador puede activarlo con su propio correo en Configuración.");

        var puerto = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
        var seguridad = (_config["Smtp:Security"] ?? "starttls").Trim().ToLowerInvariant() switch
        {
            "ssl" => "ssl",
            "none" => "none",
            _ => "starttls"
        };

        return (host, puerto, seguridad, usuario, password, from);
    }
}
