using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Gonzai_API.Services.Email;

public interface IEmailSender
{
    bool EstaConfigurado { get; }

    Task EnviarAsync(
        string para,
        string asunto,
        string html,
        string nombreRemitente,
        string? responderA,
        string nombreAdjunto,
        byte[] pdf);
}

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config)
    {
        _config = config;
    }

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(_config["Smtp:Host"])
        && !string.IsNullOrWhiteSpace(_config["Smtp:From"])
        && !string.IsNullOrWhiteSpace(_config["Smtp:User"])
        && !string.IsNullOrWhiteSpace(_config["Smtp:Password"]);

    public async Task EnviarAsync(
        string para,
        string asunto,
        string html,
        string nombreRemitente,
        string? responderA,
        string nombreAdjunto,
        byte[] pdf)
    {
        if (!EstaConfigurado)
            throw new InvalidOperationException(
                "El envío de correo no está configurado. Define Smtp:Host, Smtp:From, Smtp:User y Smtp:Password.");

        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(nombreRemitente, _config["Smtp:From"]!));
        mensaje.To.Add(MailboxAddress.Parse(para));
        if (!string.IsNullOrWhiteSpace(responderA))
            mensaje.ReplyTo.Add(new MailboxAddress(nombreRemitente, responderA));
        mensaje.Subject = asunto;

        var cuerpo = new BodyBuilder { HtmlBody = html };
        cuerpo.Attachments.Add(nombreAdjunto, pdf, new ContentType("application", "pdf"));
        mensaje.Body = cuerpo.ToMessageBody();

        var puerto = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
        var seguridad = (_config["Smtp:Security"] ?? "starttls").Trim().ToLowerInvariant() switch
        {
            "ssl" => SecureSocketOptions.SslOnConnect,
            "none" => SecureSocketOptions.None,
            _ => SecureSocketOptions.StartTls
        };

        using var cliente = new SmtpClient();
        await cliente.ConnectAsync(_config["Smtp:Host"], puerto, seguridad);
        await cliente.AuthenticateAsync(_config["Smtp:User"], _config["Smtp:Password"]);
        await cliente.SendAsync(mensaje);
        await cliente.DisconnectAsync(true);
    }
}
