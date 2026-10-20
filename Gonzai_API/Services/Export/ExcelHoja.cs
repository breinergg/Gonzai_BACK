using ClosedXML.Excel;
using Gonzai_API.Extensions;

namespace Gonzai_API.Services.Export;

internal static class ExcelHoja
{
    public const int MaxFilas = 20_000;
    private const string FormatoMoneda = "$#,##0";

    public static void Titulo(IXLWorksheet hoja, string titulo, string subtitulo, int columnas)
    {
        hoja.Cell(1, 1).Value = titulo;
        hoja.Range(1, 1, 1, columnas).Merge();
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontSize = 16;

        hoja.Cell(2, 1).Value = subtitulo;
        hoja.Range(2, 1, 2, columnas).Merge();
        hoja.Cell(2, 1).Style.Font.Italic = true;
        hoja.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
    }

    public static int Encabezados(IXLWorksheet hoja, int fila, IReadOnlyList<string> columnas)
    {
        for (var i = 0; i < columnas.Count; i++)
            hoja.Cell(fila, i + 1).Value = columnas[i];

        var rango = hoja.Range(fila, 1, fila, columnas.Count);
        rango.Style.Font.Bold = true;
        rango.Style.Font.FontColor = XLColor.White;
        rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#111111");
        hoja.SheetView.FreezeRows(fila);
        rango.SetAutoFilter();
        return fila + 1;
    }

    public static void Moneda(IXLCell celda, decimal valor)
    {
        celda.Value = valor;
        celda.Style.NumberFormat.Format = FormatoMoneda;
    }

    public static void Fecha(IXLCell celda, DateTime utc)
    {
        celda.Value = FechaNegocio.ALocal(utc);
        celda.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
    }

    public static void Totales(IXLWorksheet hoja, int fila, int columnas, IReadOnlyDictionary<int, decimal> sumas)
    {
        hoja.Cell(fila, 1).Value = "Total";
        hoja.Range(fila, 1, fila, columnas).Style.Font.SetBold();
        hoja.Range(fila, 1, fila, columnas).Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F3F3");

        foreach (var (columna, valor) in sumas)
            Moneda(hoja.Cell(fila, columna), valor);
    }

    public static void Cerrar(IXLWorksheet hoja, int columnas)
    {
        hoja.Columns(1, columnas).AdjustToContents();
        hoja.Column(1).Width = Math.Max(hoja.Column(1).Width, 18);
    }

    public static byte[] Guardar(XLWorkbook libro)
    {
        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    public static string Rango(DateOnly desde, DateOnly hasta) =>
        desde == hasta ? desde.ToString("yyyy-MM-dd") : $"{desde:yyyy-MM-dd}_{hasta:yyyy-MM-dd}";

    public static void ExigirLimite(int filas, string reporte)
    {
        if (filas > MaxFilas)
            throw new InvalidOperationException(
                $"El reporte de {reporte} tiene {filas} filas. El máximo es {MaxFilas}. Acota el rango de fechas.");
    }
}
