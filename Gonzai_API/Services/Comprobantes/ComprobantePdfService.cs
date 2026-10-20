using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Gonzai_API.Models;

namespace Gonzai_API.Services.Comprobantes;

public sealed class LineaComprobante
{
    public string Descripcion { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public decimal Precio { get; init; }
}

public sealed class DatosComprobante
{
    public bool EsAbono { get; set; }
    public bool Anulado { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string? TelefonoCliente { get; set; }
    public string? Referencia { get; set; }
    public IReadOnlyList<LineaComprobante> Lineas { get; set; } = [];
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public string? MetodoPago { get; set; }
    public decimal Saldo { get; set; }
    public string NombreComercial { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Ciudad { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public byte[]? Logo { get; set; }
}

public class ComprobantePdfService
{
    private static readonly CultureInfo Cultura = new("es-CO");

    public byte[] Generar(DatosComprobante datos, string formato)
    {
        var termico = formato != FormatosComprobante.MediaCarta;

        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                if (termico)
                {
                    pagina.ContinuousSize(80, Unit.Millimetre);
                    pagina.MarginHorizontal(4, Unit.Millimetre);
                    pagina.MarginVertical(5, Unit.Millimetre);
                    pagina.DefaultTextStyle(t => t.FontSize(8).FontColor(Colors.Black));
                }
                else
                {
                    pagina.Size(140, 216, Unit.Millimetre);
                    pagina.Margin(12, Unit.Millimetre);
                    pagina.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Black));
                }

                pagina.Content().Element(c => Dibujar(c, datos, termico));
            });
        }).GeneratePdf();
    }

    private static void Dibujar(IContainer contenedor, DatosComprobante datos, bool termico)
    {
        contenedor.Column(col =>
        {
            col.Spacing(termico ? 3 : 6);

            if (datos.Logo is { Length: > 0 })
            {
                col.Item().AlignCenter().MaxWidth(48, Unit.Millimetre).MaxHeight(18, Unit.Millimetre)
                    .Image(datos.Logo).FitArea();
            }

            col.Item().AlignCenter().Text(datos.NombreComercial).Bold().FontSize(termico ? 11 : 16);
            var pie = string.Join(" · ", new[] { datos.Telefono, datos.Ciudad }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (pie.Length > 0)
                col.Item().AlignCenter().Text(pie).FontSize(termico ? 7 : 9).FontColor(Colors.Grey.Darken2);

            col.Item().LineHorizontal(0.6f).LineColor(Colors.Grey.Lighten1);
            col.Item().AlignCenter().Text(datos.Titulo).Bold();
            if (datos.Anulado)
                col.Item().AlignCenter().Text("ANULADO").Bold().FontColor(Colors.Red.Medium);

            col.Item().Text(texto =>
            {
                texto.Span(datos.Numero).Bold();
                texto.Span("  ·  ");
                texto.Span(Fecha(datos.FechaUtc));
            });
            col.Item().Text(texto =>
            {
                texto.Span("Cliente: ").Bold();
                texto.Span(datos.Cliente);
            });

            if (!string.IsNullOrWhiteSpace(datos.Referencia))
                col.Item().Text(datos.Referencia);

            if (datos.Lineas.Count > 0)
            {
                col.Item().LineHorizontal(0.4f).LineColor(Colors.Grey.Lighten1);
                foreach (var linea in datos.Lineas)
                {
                    col.Item().Row(fila =>
                    {
                        fila.RelativeItem().Text($"{linea.Cantidad}  {linea.Descripcion}");
                        fila.ConstantItem(70).AlignRight().Text(Pesos(linea.Cantidad * linea.Precio));
                    });
                }
            }

            col.Item().LineHorizontal(0.6f).LineColor(Colors.Grey.Lighten1);
            FilaMonto(col, datos.EsAbono ? "Total de la venta" : "Total", datos.Total, false);
            var pagado = datos.EsAbono
                ? (string.IsNullOrWhiteSpace(datos.MetodoPago) ? "Abono" : $"Abono ({datos.MetodoPago})")
                : (string.IsNullOrWhiteSpace(datos.MetodoPago) ? "Pagado" : $"Pagado ({datos.MetodoPago})");
            FilaMonto(col, pagado, datos.Pagado, false);
            FilaMonto(col, datos.EsAbono ? "Saldo de la venta" : "Saldo pendiente", datos.Saldo, true);

            col.Item().PaddingTop(4).AlignCenter().Text(datos.Mensaje).Italic().FontSize(termico ? 7 : 9);
        });
    }

    private static void FilaMonto(ColumnDescriptor col, string etiqueta, decimal valor, bool fuerte)
    {
        col.Item().Row(fila =>
        {
            var texto = fila.RelativeItem().Text(etiqueta);
            var monto = fila.ConstantItem(80).AlignRight().Text(Pesos(valor));
            if (fuerte)
            {
                texto.Bold();
                monto.Bold();
            }
        });
    }

    private static string Pesos(decimal valor) => valor.ToString("C0", Cultura);

    private static string Fecha(DateTime utc) =>
        Extensions.FechaNegocio.ALocal(utc).ToString("dd/MM/yyyy HH:mm", Cultura);
}
