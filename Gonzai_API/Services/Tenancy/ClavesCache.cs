namespace Gonzai_API.Services.Tenancy;

/// <summary>
/// Claves de la caché del dashboard. Todas llevan el Id de la empresa porque la caché
/// es un único diccionario en memoria compartido por todas las empresas del proceso.
/// </summary>
public static class ClavesCache
{
    public const string MayorDeuda       = "mayor-deuda";
    public const string TotalDeuda       = "total-deuda";
    public const string ClientesConDeuda = "clientes-con-deuda";
    public const string ClientesActivos  = "clientes-activos";
    public const string ResumenMensual   = "resumen-mensual";
    public const string ProductosActivos = "productos-activos";

    private static readonly string[] Todas =
    [
        MayorDeuda, TotalDeuda, ClientesConDeuda, ClientesActivos, ResumenMensual, ProductosActivos
    ];

    public static string Dashboard(int empresaId, string sufijo) => $"dashboard:{empresaId}:{sufijo}";

    public static IEnumerable<string> TodasDelDashboard(int empresaId) =>
        Todas.Select(s => Dashboard(empresaId, s));
}
