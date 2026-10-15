namespace Gonzai_API.Models;

public static class EstadosDocumento
{
    public const string Activa  = "Activa";
    public const string Anulada = "Anulada";
}

public static class EstadosPagoVenta
{
    public const string Pagada    = "Pagada";
    public const string Parcial   = "Parcial";
    public const string Pendiente = "Pendiente";

    public static string Calcular(decimal total, decimal montoPagado) =>
        montoPagado >= total ? Pagada :
        montoPagado > 0      ? Parcial :
                               Pendiente;
}

public static class EstadosPago
{
    public const string Activo  = "Activo";
    public const string Anulado = "Anulado";
}

public static class MetodosPago
{
    public const string Patron = "^(efectivo|transferencia|tarjeta|nequi|daviplata|otro)$";
}

public static class TiposMovimientoInventario
{
    public const string Entrada = "entrada";
    public const string Salida  = "salida";
}

public static class MotivosMovimientoInventario
{
    public const string CompraProveedor   = "CompraProveedor";
    public const string VentaCliente      = "VentaCliente";
    public const string AjusteManual      = "AjusteManual";
    public const string AnulacionCompra   = "AnulacionCompra";
    public const string AnulacionVenta    = "AnulacionVenta";
    public const string InventarioInicial = "InventarioInicial";
}
