namespace Gonzai_API.Services.Interfaces;

/// <summary>
/// Descarga de comprobantes por enlace público. No hay sesión, así que la empresa se deduce
/// del propio token y de ahí sale la base de datos donde buscar.
/// </summary>
public interface IComprobantePublicoService
{
    Task<(byte[] Pdf, string NombreArchivo)?> PorTokenAsync(string token, CancellationToken ct = default);
}
