namespace Gonzai_API.Extensions;

/// <summary>
/// Las fechas se guardan en UTC, pero "hoy" y "este mes" se calculan en la hora local del negocio.
/// </summary>
public static class FechaNegocio
{
    private static readonly TimeZoneInfo Zona = ObtenerZona();

    public static DateTime NormalizarUtc(DateTime? fecha) => fecha switch
    {
        null                                  => DateTime.UtcNow,
        { Kind: DateTimeKind.Utc } f          => f,
        { Kind: DateTimeKind.Local } f        => f.ToUniversalTime(),
        { } f                                 => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(f, DateTimeKind.Unspecified), Zona)
    };

    public static DateOnly Hoy() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona));

    public static DateTime ALocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);

    /// <summary>Rango UTC [desde, hasta) que cubre los días locales indicados (ambos incluidos).</summary>
    public static (DateTime DesdeUtc, DateTime HastaUtc) RangoUtc(DateOnly desde, DateOnly hasta) =>
        (InicioDiaUtc(desde), InicioDiaUtc(hasta.AddDays(1)));

    private static DateTime InicioDiaUtc(DateOnly dia) =>
        TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), Zona);

    private static TimeZoneInfo ObtenerZona()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
