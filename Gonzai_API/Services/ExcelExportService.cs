using ClosedXML.Excel;
using Gonzai_API.Data;
using Gonzai_API.Extensions;
using Gonzai_API.Models;
using Gonzai_API.Services.Export;
using Gonzai_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class ExcelExportService : IExcelExportService
{
    private readonly AppDbContext _context;

    public ExcelExportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ArchivoExcel> LibroVentasAsync(
        DateOnly? desde, DateOnly? hasta, int? clienteId, bool soloPendientes, bool incluirAnuladas, bool incluirCostos)
    {
        var (inicio, fin) = ResolverRango(desde, hasta, mesCompleto: true);
        var (desdeUtc, hastaUtc) = FechaNegocio.RangoUtc(inicio, fin);

        var query = _context.VentasCliente
            .AsNoTracking()
            .Where(v => v.Fecha >= desdeUtc && v.Fecha < hastaUtc);

        if (clienteId.HasValue)
            query = query.Where(v => v.ClienteId == clienteId.Value);

        if (soloPendientes)
            query = query.Where(v => v.Estado == EstadosDocumento.Activa && v.SaldoPendiente > 0);
        else if (!incluirAnuladas)
            query = query.Where(v => v.Estado == EstadosDocumento.Activa);

        var ventas = await query
            .OrderBy(v => v.Fecha)
            .ThenBy(v => v.Id)
            .Select(v => new VentaFila(
                v.Id,
                v.Fecha,
                v.Cliente.Nombre,
                v.Descripcion,
                v.Total,
                v.MontoPagado,
                v.SaldoPendiente,
                v.EstadoPago,
                v.Estado,
                v.Usuario.Nombre,
                v.Detalles.Sum(d => d.CostoTotal)))
            .ToListAsync();

        ExcelHoja.ExigirLimite(ventas.Count, "libro de ventas");

        var columnas = incluirCostos
            ? ColumnasLibro.Concat(["Costo", "Ganancia"]).ToArray()
            : ColumnasLibro;

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Ventas");
        ExcelHoja.Titulo(hoja, "Libro de ventas",
            $"Del {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy} · Generado {FechaNegocio.ALocal(DateTime.UtcNow):dd/MM/yyyy HH:mm}",
            columnas.Length);

        var fila = ExcelHoja.Encabezados(hoja, 4, columnas);

        foreach (var venta in ventas)
        {
            hoja.Cell(fila, 1).Value = venta.Id;
            ExcelHoja.Fecha(hoja.Cell(fila, 2), venta.Fecha);
            hoja.Cell(fila, 3).Value = venta.Cliente;
            hoja.Cell(fila, 4).Value = venta.Descripcion ?? string.Empty;
            ExcelHoja.Moneda(hoja.Cell(fila, 5), venta.Total);
            ExcelHoja.Moneda(hoja.Cell(fila, 6), venta.MontoPagado);
            ExcelHoja.Moneda(hoja.Cell(fila, 7), venta.SaldoPendiente);
            hoja.Cell(fila, 8).Value = venta.EstadoPago;
            hoja.Cell(fila, 9).Value = venta.Estado;
            hoja.Cell(fila, 10).Value = venta.Usuario;

            if (incluirCostos)
            {
                ExcelHoja.Moneda(hoja.Cell(fila, 11), venta.Costo);
                ExcelHoja.Moneda(hoja.Cell(fila, 12), venta.Total - venta.Costo);
            }

            fila++;
        }

        if (ventas.Count == 0)
            hoja.Cell(fila, 1).Value = "Sin ventas en el rango indicado.";
        else
        {
            var sumas = new Dictionary<int, decimal>
            {
                [5] = ventas.Sum(v => v.Total),
                [6] = ventas.Sum(v => v.MontoPagado),
                [7] = ventas.Sum(v => v.SaldoPendiente)
            };
            if (incluirCostos)
            {
                sumas[11] = ventas.Sum(v => v.Costo);
                sumas[12] = ventas.Sum(v => v.Total - v.Costo);
            }

            ExcelHoja.Totales(hoja, fila, columnas.Length, sumas);
        }

        ExcelHoja.Cerrar(hoja, columnas.Length);

        return new ArchivoExcel(ExcelHoja.Guardar(libro), $"libro-ventas_{ExcelHoja.Rango(inicio, fin)}.xlsx");
    }

    public async Task<ArchivoExcel> CarteraAsync()
    {
        var clientes = await _context.Clientes
            .AsNoTracking()
            .Where(c => c.Activo)
            .Select(c => new
            {
                c.Id,
                c.Nombre,
                c.Telefono,
                c.Direccion,
                TotalComprado = c.Ventas.Where(v => v.Estado == EstadosDocumento.Activa).Sum(v => (decimal?)v.Total) ?? 0m,
                TotalPagado   = c.Ventas.Where(v => v.Estado == EstadosDocumento.Activa).Sum(v => (decimal?)v.MontoPagado) ?? 0m,
                Saldo         = c.Ventas.Where(v => v.Estado == EstadosDocumento.Activa).Sum(v => (decimal?)v.SaldoPendiente) ?? 0m,
                Pendientes    = c.Ventas.Count(v => v.Estado == EstadosDocumento.Activa && v.SaldoPendiente > 0)
            })
            .Where(c => c.Saldo > 0)
            .OrderByDescending(c => c.Saldo)
            .ThenBy(c => c.Nombre)
            .ToListAsync();

        ExcelHoja.ExigirLimite(clientes.Count, "cartera");

        var columnas = new[]
        {
            "Cliente", "Teléfono", "Dirección", "Total comprado", "Total pagado", "Saldo", "Ventas pendientes"
        };

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Cartera");
        ExcelHoja.Titulo(hoja, "Cuentas por cobrar",
            $"Clientes con saldo · Generado {FechaNegocio.ALocal(DateTime.UtcNow):dd/MM/yyyy HH:mm}",
            columnas.Length);

        var fila = ExcelHoja.Encabezados(hoja, 4, columnas);

        foreach (var cliente in clientes)
        {
            hoja.Cell(fila, 1).Value = cliente.Nombre;
            hoja.Cell(fila, 2).Value = cliente.Telefono ?? string.Empty;
            hoja.Cell(fila, 3).Value = cliente.Direccion ?? string.Empty;
            ExcelHoja.Moneda(hoja.Cell(fila, 4), cliente.TotalComprado);
            ExcelHoja.Moneda(hoja.Cell(fila, 5), cliente.TotalPagado);
            ExcelHoja.Moneda(hoja.Cell(fila, 6), cliente.Saldo);
            hoja.Cell(fila, 7).Value = cliente.Pendientes;
            fila++;
        }

        if (clientes.Count == 0)
            hoja.Cell(fila, 1).Value = "No hay clientes con saldo pendiente.";
        else
        {
            ExcelHoja.Totales(hoja, fila, columnas.Length, new Dictionary<int, decimal>
            {
                [4] = clientes.Sum(c => c.TotalComprado),
                [5] = clientes.Sum(c => c.TotalPagado),
                [6] = clientes.Sum(c => c.Saldo)
            });
        }

        ExcelHoja.Cerrar(hoja, columnas.Length);
        return new ArchivoExcel(ExcelHoja.Guardar(libro), $"cartera_{FechaNegocio.Hoy():yyyy-MM-dd}.xlsx");
    }

    public async Task<ArchivoExcel?> EstadoCuentaAsync(int clienteId)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clienteId);

        if (cliente is null) return null;

        var ventas = await _context.VentasCliente
            .AsNoTracking()
            .Where(v => v.ClienteId == clienteId && v.Estado == EstadosDocumento.Activa)
            .OrderBy(v => v.Fecha)
            .ThenBy(v => v.Id)
            .ToListAsync();

        var pendientes = ventas.Where(v => v.SaldoPendiente > 0).ToList();

        var pagos = await _context.PagosCliente
            .AsNoTracking()
            .Include(p => p.Usuario)
            .Include(p => p.VentaCliente)
            .Where(p => p.ClienteId == clienteId)
            .OrderBy(p => p.Fecha)
            .ThenBy(p => p.Id)
            .ToListAsync();

        using var libro = new XLWorkbook();
        EscribirResumenCliente(libro, cliente, ventas);
        EscribirVentasPendientes(libro, pendientes);
        EscribirAbonosCliente(libro, pagos);

        var nombre = string.Concat(cliente.Nombre.Where(c => char.IsLetterOrDigit(c) || c == ' ')).Trim();
        if (string.IsNullOrWhiteSpace(nombre)) nombre = $"cliente-{cliente.Id}";

        return new ArchivoExcel(ExcelHoja.Guardar(libro), $"estado-cuenta_{nombre.Replace(' ', '-')}.xlsx");
    }

    public async Task<ArchivoExcel> AbonosAsync(DateOnly? desde, DateOnly? hasta, int? clienteId, bool incluirAnulados)
    {
        var (inicio, fin) = ResolverRango(desde, hasta, mesCompleto: false);
        var (desdeUtc, hastaUtc) = FechaNegocio.RangoUtc(inicio, fin);

        var query = _context.PagosCliente
            .AsNoTracking()
            .Where(p => p.Fecha >= desdeUtc && p.Fecha < hastaUtc);

        if (clienteId.HasValue)
            query = query.Where(p => p.ClienteId == clienteId.Value);

        if (!incluirAnulados)
            query = query.Where(p => p.Estado == EstadosPago.Activo);

        var pagos = await query
            .OrderBy(p => p.Fecha)
            .ThenBy(p => p.Id)
            .Select(p => new AbonoFila(
                p.Fecha,
                p.Cliente.Nombre,
                p.VentaClienteId,
                p.Monto,
                p.MetodoPago,
                p.Usuario.Nombre,
                p.VentaCliente.SaldoPendiente,
                p.Estado,
                p.Descripcion))
            .ToListAsync();

        ExcelHoja.ExigirLimite(pagos.Count, "abonos");

        var columnas = new[]
        {
            "Fecha", "Cliente", "Venta #", "Monto", "Método", "Usuario", "Saldo actual de la venta", "Estado", "Descripción"
        };

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Abonos");
        ExcelHoja.Titulo(hoja, "Abonos recibidos",
            $"Del {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy} · El saldo es el que la venta tiene hoy, no el que quedó al registrar el abono · Generado {FechaNegocio.ALocal(DateTime.UtcNow):dd/MM/yyyy HH:mm}",
            columnas.Length);

        var fila = ExcelHoja.Encabezados(hoja, 4, columnas);

        foreach (var pago in pagos)
        {
            ExcelHoja.Fecha(hoja.Cell(fila, 1), pago.Fecha);
            hoja.Cell(fila, 2).Value = pago.Cliente;
            hoja.Cell(fila, 3).Value = pago.VentaId;
            ExcelHoja.Moneda(hoja.Cell(fila, 4), pago.Monto);
            hoja.Cell(fila, 5).Value = pago.Metodo;
            hoja.Cell(fila, 6).Value = pago.Usuario;
            ExcelHoja.Moneda(hoja.Cell(fila, 7), pago.SaldoActualVenta);
            hoja.Cell(fila, 8).Value = pago.Estado;
            hoja.Cell(fila, 9).Value = pago.Descripcion ?? string.Empty;
            fila++;
        }

        if (pagos.Count == 0)
            hoja.Cell(fila, 1).Value = "Sin abonos en el rango indicado.";
        else
            ExcelHoja.Totales(hoja, fila, columnas.Length, new Dictionary<int, decimal> { [4] = pagos.Sum(p => p.Monto) });

        ExcelHoja.Cerrar(hoja, columnas.Length);
        return new ArchivoExcel(ExcelHoja.Guardar(libro), $"abonos_{ExcelHoja.Rango(inicio, fin)}.xlsx");
    }

    private static readonly string[] ColumnasLibro =
    [
        "Venta #", "Fecha", "Cliente", "Descripción", "Total", "Pagado", "Saldo", "Estado pago", "Estado", "Usuario"
    ];

    private static (DateOnly Desde, DateOnly Hasta) ResolverRango(DateOnly? desde, DateOnly? hasta, bool mesCompleto)
    {
        if (desde.HasValue || hasta.HasValue)
        {
            var inicio = desde ?? DateOnly.MinValue;
            var fin = hasta ?? FechaNegocio.Hoy();
            if (inicio > fin)
                throw new InvalidOperationException("La fecha 'desde' no puede ser mayor que 'hasta'.");
            return (inicio, fin);
        }

        var hoy = FechaNegocio.Hoy();
        return mesCompleto
            ? (new DateOnly(hoy.Year, hoy.Month, 1), hoy)
            : (hoy, hoy);
    }

    private static void EscribirResumenCliente(XLWorkbook libro, Cliente cliente, List<VentaCliente> ventas)
    {
        var hoja = libro.Worksheets.Add("Resumen");
        var columnas = 2;
        ExcelHoja.Titulo(hoja, "Estado de cuenta",
            $"Generado {FechaNegocio.ALocal(DateTime.UtcNow):dd/MM/yyyy HH:mm}", columnas);

        var datos = new (string Etiqueta, string Valor)[]
        {
            ("Cliente", cliente.Nombre),
            ("Teléfono", cliente.Telefono ?? "—"),
            ("Dirección", cliente.Direccion ?? "—"),
            ("Total comprado", ventas.Sum(v => v.Total).ToString("N0")),
            ("Total pagado", ventas.Sum(v => v.MontoPagado).ToString("N0")),
            ("Saldo", ventas.Sum(v => v.SaldoPendiente).ToString("N0")),
            ("Estado", ventas.Sum(v => v.SaldoPendiente) <= 0 ? "En paz y salvo" : "Con saldo")
        };

        var fila = 4;
        foreach (var (etiqueta, valor) in datos)
        {
            hoja.Cell(fila, 1).Value = etiqueta;
            hoja.Cell(fila, 1).Style.Font.SetBold();
            hoja.Cell(fila, 2).Value = valor;
            fila++;
        }

        ExcelHoja.Cerrar(hoja, columnas);
    }

    private static void EscribirVentasPendientes(XLWorkbook libro, List<VentaCliente> pendientes)
    {
        var columnas = new[] { "Venta #", "Fecha", "Total", "Pagado", "Saldo", "Estado pago" };
        var hoja = libro.Worksheets.Add("Ventas pendientes");
        ExcelHoja.Titulo(hoja, "Ventas con saldo", $"{pendientes.Count} venta(s)", columnas.Length);

        var fila = ExcelHoja.Encabezados(hoja, 4, columnas);
        foreach (var venta in pendientes)
        {
            hoja.Cell(fila, 1).Value = venta.Id;
            ExcelHoja.Fecha(hoja.Cell(fila, 2), venta.Fecha);
            ExcelHoja.Moneda(hoja.Cell(fila, 3), venta.Total);
            ExcelHoja.Moneda(hoja.Cell(fila, 4), venta.MontoPagado);
            ExcelHoja.Moneda(hoja.Cell(fila, 5), venta.SaldoPendiente);
            hoja.Cell(fila, 6).Value = venta.EstadoPago;
            fila++;
        }

        if (pendientes.Count == 0)
            hoja.Cell(fila, 1).Value = "El cliente no tiene ventas pendientes.";
        else
        {
            ExcelHoja.Totales(hoja, fila, columnas.Length, new Dictionary<int, decimal>
            {
                [3] = pendientes.Sum(v => v.Total),
                [4] = pendientes.Sum(v => v.MontoPagado),
                [5] = pendientes.Sum(v => v.SaldoPendiente)
            });
        }

        ExcelHoja.Cerrar(hoja, columnas.Length);
    }

    private static void EscribirAbonosCliente(XLWorkbook libro, List<PagoCliente> pagos)
    {
        var columnas = new[]
        {
            "Fecha", "Venta #", "Monto", "Método", "Usuario", "Estado", "Descripción", "Saldo actual de la venta"
        };
        var hoja = libro.Worksheets.Add("Abonos");
        ExcelHoja.Titulo(hoja, "Abonos del cliente",
            "El saldo es el que la venta tiene hoy.", columnas.Length);

        var fila = ExcelHoja.Encabezados(hoja, 4, columnas);
        foreach (var pago in pagos)
        {
            ExcelHoja.Fecha(hoja.Cell(fila, 1), pago.Fecha);
            hoja.Cell(fila, 2).Value = pago.VentaClienteId;
            ExcelHoja.Moneda(hoja.Cell(fila, 3), pago.Monto);
            hoja.Cell(fila, 4).Value = pago.MetodoPago;
            hoja.Cell(fila, 5).Value = pago.Usuario.Nombre;
            hoja.Cell(fila, 6).Value = pago.Estado;
            hoja.Cell(fila, 7).Value = pago.Descripcion ?? string.Empty;
            ExcelHoja.Moneda(hoja.Cell(fila, 8), pago.VentaCliente.SaldoPendiente);
            fila++;
        }

        if (pagos.Count == 0)
            hoja.Cell(fila, 1).Value = "El cliente no tiene abonos.";
        else
        {
            ExcelHoja.Totales(hoja, fila, columnas.Length, new Dictionary<int, decimal>
            {
                [3] = pagos.Where(p => p.Estado == EstadosPago.Activo).Sum(p => p.Monto)
            });
        }

        ExcelHoja.Cerrar(hoja, columnas.Length);
    }

    private sealed record VentaFila(
        int Id, DateTime Fecha, string Cliente, string? Descripcion,
        decimal Total, decimal MontoPagado, decimal SaldoPendiente,
        string EstadoPago, string Estado, string Usuario, decimal Costo);

    private sealed record AbonoFila(
        DateTime Fecha, string Cliente, int VentaId, decimal Monto, string Metodo,
        string Usuario, decimal SaldoActualVenta, string Estado, string? Descripcion);
}
