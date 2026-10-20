namespace Gonzai_API.Services.Interfaces;

public sealed record ArchivoExcel(byte[] Contenido, string NombreArchivo);

public interface IExcelExportService
{
    /// <summary>
    /// Libro de ventas. Sin fechas, toma el mes actual (hora de Colombia).
    /// <paramref name="incluirCostos"/> agrega costo y ganancia (solo Admin).
    /// </summary>
    Task<ArchivoExcel> LibroVentasAsync(
        DateOnly? desde, DateOnly? hasta, int? clienteId, bool soloPendientes, bool incluirAnuladas, bool incluirCostos);

    /// <summary>Clientes activos con saldo pendiente.</summary>
    Task<ArchivoExcel> CarteraAsync();

    /// <summary>Resumen del cliente, ventas pendientes y abonos. Null si el cliente no existe.</summary>
    Task<ArchivoExcel?> EstadoCuentaAsync(int clienteId);

    /// <summary>
    /// Abonos del rango. Sin fechas, toma el día de hoy (hora de Colombia).
    /// El saldo es el saldo actual de la venta, no el que quedó justo después de ese abono.
    /// </summary>
    Task<ArchivoExcel> AbonosAsync(DateOnly? desde, DateOnly? hasta, int? clienteId, bool incluirAnulados);
}
