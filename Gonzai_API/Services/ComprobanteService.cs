using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using Gonzai_API.Data;
using Gonzai_API.DTOs.Comprobante;
using Gonzai_API.Extensions;
using Gonzai_API.Models;
using Gonzai_API.Services.Comprobantes;
using Gonzai_API.Services.Email;
using Gonzai_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class ComprobanteService : IComprobanteService
{
    private static readonly CultureInfo Cultura = new("es-CO");
    private static readonly TimeSpan Vigencia = TimeSpan.FromDays(15);

    private readonly AppDbContext _context;
    private readonly ComprobantePdfService _pdf;
    private readonly IEmailSender _email;

    public ComprobanteService(AppDbContext context, ComprobantePdfService pdf, IEmailSender email)
    {
        _context = context;
        _pdf = pdf;
        _email = email;
    }

    public async Task<(byte[] Pdf, string NombreArchivo)> VentaPdfAsync(int ventaId, string? formato)
    {
        var (datos, nombre, fmt) = await DatosVentaAsync(ventaId, formato);
        return (_pdf.Generar(datos, fmt), nombre);
    }

    public async Task<(byte[] Pdf, string NombreArchivo)> AbonoPdfAsync(int pagoId, string? formato)
    {
        var (datos, nombre, fmt) = await DatosAbonoAsync(pagoId, formato);
        return (_pdf.Generar(datos, fmt), nombre);
    }

    public async Task<CompartirComprobanteDto> CompartirVentaAsync(int ventaId, string? formato, bool regenerar, string baseUrl)
    {
        var (datos, _, fmt) = await DatosVentaAsync(ventaId, formato);
        var enlace = await EnlaceAsync(TiposComprobante.Venta, ventaId, null, fmt, regenerar);
        return Compartir(datos, enlace, baseUrl);
    }

    public async Task<CompartirComprobanteDto> CompartirAbonoAsync(int pagoId, string? formato, bool regenerar, string baseUrl)
    {
        var (datos, _, fmt) = await DatosAbonoAsync(pagoId, formato);
        var enlace = await EnlaceAsync(TiposComprobante.Abono, null, pagoId, fmt, regenerar);
        return Compartir(datos, enlace, baseUrl);
    }

    public Task EnviarVentaAsync(int ventaId, string correo, string? formato, string baseUrl) =>
        Enviar(ventaId, null, correo, formato, baseUrl);

    public Task EnviarAbonoAsync(int pagoId, string correo, string? formato, string baseUrl) =>
        Enviar(null, pagoId, correo, formato, baseUrl);

    public async Task<(byte[] Pdf, string NombreArchivo)?> PorTokenAsync(string token)
    {
        var enlace = await _context.ComprobantesEnlace
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Token == token);

        if (enlace is null || enlace.ExpiraEn <= DateTime.UtcNow)
            return null;

        if (enlace.Tipo == TiposComprobante.Abono && enlace.PagoClienteId is int pagoId)
            return await AbonoPdfAsync(pagoId, enlace.Formato);

        if (enlace.VentaClienteId is int ventaId)
            return await VentaPdfAsync(ventaId, enlace.Formato);

        return null;
    }

    private async Task Enviar(int? ventaId, int? pagoId, string correo, string? formato, string baseUrl)
    {
        var (datos, nombre, fmt) = ventaId is int id
            ? await DatosVentaAsync(id, formato)
            : await DatosAbonoAsync(pagoId!.Value, formato);

        var enlace = await EnlaceAsync(
            ventaId is null ? TiposComprobante.Abono : TiposComprobante.Venta,
            ventaId, pagoId, fmt, regenerar: false);

        var negocio = await _context.Negocios.AsNoTracking().OrderBy(n => n.Id).FirstOrDefaultAsync();
        var url = UrlPublica(baseUrl, enlace.Token);
        var pdf = _pdf.Generar(datos, fmt);
        var vence = FechaNegocio.ALocal(enlace.ExpiraEn).ToString("dd/MM/yyyy", Cultura);
        var html = $"""
            <p>Hola {WebUtility.HtmlEncode(datos.Cliente)},</p>
            <p>{WebUtility.HtmlEncode(Texto(datos))}</p>
            <p><a href="{WebUtility.HtmlEncode(url)}">Ver comprobante</a>. El enlace vence el {vence}.</p>
            <p>{WebUtility.HtmlEncode(datos.Mensaje)}</p>
            """;

        await _email.EnviarAsync(
            correo.Trim(),
            $"{datos.Numero} · {datos.NombreComercial}",
            html,
            datos.NombreComercial,
            negocio?.CorreoRespuesta,
            nombre,
            pdf);
    }

    private async Task<(DatosComprobante Datos, string Nombre, string Formato)> DatosVentaAsync(int ventaId, string? formato)
    {
        var venta = await _context.VentasCliente
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.Pagos)
            .Include(v => v.Detalles).ThenInclude(d => d.ProductoVariante).ThenInclude(p => p.Producto)
            .FirstOrDefaultAsync(v => v.Id == ventaId)
            ?? throw new KeyNotFoundException($"No se encontró la venta con Id {ventaId}.");

        var negocio = await NegocioAsync();
        var metodos = venta.Pagos
            .Where(p => p.Estado == EstadosPago.Activo)
            .Select(p => p.MetodoPago)
            .Distinct()
            .ToList();

        var datos = Base(negocio, esAbono: false, venta.Estado == EstadosDocumento.Anulada);
        datos.Titulo = "COMPROBANTE DE VENTA";
        datos.Numero = $"Venta #{venta.Id}";
        datos.FechaUtc = venta.Fecha;
        datos.Cliente = venta.Cliente.Nombre;
        datos.TelefonoCliente = venta.Cliente.Telefono;
        datos.Lineas = venta.Detalles
            .OrderBy(d => d.Id)
            .Select(d => new LineaComprobante
            {
                Descripcion = d.ProductoVariante.NombreCompleto,
                Cantidad = d.Cantidad,
                Precio = d.PrecioVentaUnitario
            })
            .ToList();
        datos.Total = venta.Total;
        datos.Pagado = venta.MontoPagado;
        datos.MetodoPago = metodos.Count == 0 ? null : string.Join(", ", metodos);
        datos.Saldo = venta.SaldoPendiente;

        return (datos, $"comprobante-venta-{venta.Id}.pdf", ResolverFormato(formato, negocio.FormatoPredeterminado));
    }

    private async Task<(DatosComprobante Datos, string Nombre, string Formato)> DatosAbonoAsync(int pagoId, string? formato)
    {
        var pago = await _context.PagosCliente
            .AsNoTracking()
            .Include(p => p.Cliente)
            .Include(p => p.VentaCliente)
            .FirstOrDefaultAsync(p => p.Id == pagoId)
            ?? throw new KeyNotFoundException($"No se encontró el abono con Id {pagoId}.");

        var negocio = await NegocioAsync();
        var datos = Base(negocio, esAbono: true, pago.Estado == EstadosPago.Anulado);
        datos.Titulo = "COMPROBANTE DE ABONO";
        datos.Numero = $"Abono #{pago.Id}";
        datos.FechaUtc = pago.Fecha;
        datos.Cliente = pago.Cliente.Nombre;
        datos.TelefonoCliente = pago.Cliente.Telefono;
        datos.Referencia = $"Abono a la venta #{pago.VentaClienteId}";
        datos.Total = pago.VentaCliente.Total;
        datos.Pagado = pago.Monto;
        datos.MetodoPago = pago.MetodoPago;
        datos.Saldo = pago.VentaCliente.SaldoPendiente;

        return (datos, $"comprobante-abono-{pago.Id}.pdf", ResolverFormato(formato, negocio.FormatoPredeterminado));
    }

    private static DatosComprobante Base(Negocio negocio, bool esAbono, bool anulado) => new()
    {
        EsAbono = esAbono,
        Anulado = anulado,
        NombreComercial = negocio.NombreComercial,
        Telefono = negocio.Telefono,
        Ciudad = negocio.Ciudad,
        Mensaje = negocio.Mensaje,
        Logo = negocio.Logo
    };

    private async Task<Negocio> NegocioAsync() =>
        await _context.Negocios.AsNoTracking().OrderBy(n => n.Id).FirstOrDefaultAsync() ?? new Negocio();

    private async Task<ComprobanteEnlace> EnlaceAsync(string tipo, int? ventaId, int? pagoId, string formato, bool regenerar)
    {
        var ahora = DateTime.UtcNow;
        var consulta = _context.ComprobantesEnlace.Where(e => e.Tipo == tipo && e.Formato == formato);
        consulta = ventaId is int venta
            ? consulta.Where(e => e.VentaClienteId == venta)
            : consulta.Where(e => e.PagoClienteId == pagoId);

        if (regenerar)
            _context.ComprobantesEnlace.RemoveRange(await consulta.ToListAsync());
        else
        {
            var vigente = await consulta.FirstOrDefaultAsync(e => e.ExpiraEn > ahora);
            if (vigente is not null) return vigente;
        }

        var enlace = new ComprobanteEnlace
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_'),
            Tipo = tipo,
            VentaClienteId = ventaId,
            PagoClienteId = pagoId,
            Formato = formato,
            ExpiraEn = ahora.Add(Vigencia)
        };
        _context.ComprobantesEnlace.Add(enlace);
        await _context.SaveChangesAsync();
        return enlace;
    }

    private static CompartirComprobanteDto Compartir(DatosComprobante datos, ComprobanteEnlace enlace, string baseUrl)
    {
        var url = UrlPublica(baseUrl, enlace.Token);
        var mensaje = $"{Texto(datos)} Comprobante: {url}";
        var telefono = AWhatsapp(datos.TelefonoCliente);

        return new CompartirComprobanteDto
        {
            Url = url,
            Mensaje = mensaje,
            WhatsappUrl = telefono is null ? null : $"https://wa.me/{telefono}?text={Uri.EscapeDataString(mensaje)}",
            ExpiraEn = enlace.ExpiraEn,
            Formato = enlace.Formato
        };
    }

    private static string Texto(DatosComprobante datos)
    {
        if (datos.EsAbono)
            return $"Hola, {datos.Cliente}. Registramos tu abono de {Pesos(datos.Pagado)} en {datos.NombreComercial}. {datos.Referencia}. Saldo de la venta: {Pesos(datos.Saldo)}.";

        return $"Hola, {datos.Cliente}. Tu {datos.Numero} en {datos.NombreComercial} fue por {Pesos(datos.Total)}. Saldo: {Pesos(datos.Saldo)}.";
    }

    private static string? AWhatsapp(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return null;
        var digitos = new string(telefono.Where(char.IsDigit).ToArray());
        if (digitos.StartsWith("00")) digitos = digitos[2..];
        if (digitos.Length == 10) digitos = "57" + digitos;
        return digitos.Length is >= 11 and <= 15 ? digitos : null;
    }

    private static string Pesos(decimal valor) => valor.ToString("C0", Cultura);

    private static string ResolverFormato(string? solicitado, string predeterminado) =>
        FormatosComprobante.Normalizar(string.IsNullOrWhiteSpace(solicitado) ? predeterminado : solicitado);

    private static string UrlPublica(string baseUrl, string token) =>
        $"{baseUrl.TrimEnd('/')}/api/comprobantes/{token}";
}
