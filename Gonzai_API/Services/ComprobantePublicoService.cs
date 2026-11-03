using Gonzai_API.Services.Comprobantes;
using Gonzai_API.Services.Email;
using Gonzai_API.Services.Interfaces;
using Gonzai_API.Services.Tenancy;

namespace Gonzai_API.Services;

public class ComprobantePublicoService : IComprobantePublicoService
{
    private readonly ITenantStore _store;
    private readonly ITenantDbFactory _factory;
    private readonly ComprobantePdfService _pdf;
    private readonly IEmailSender _email;

    public ComprobantePublicoService(
        ITenantStore store, ITenantDbFactory factory, ComprobantePdfService pdf, IEmailSender email)
    {
        _store   = store;
        _factory = factory;
        _pdf     = pdf;
        _email   = email;
    }

    public async Task<(byte[] Pdf, string NombreArchivo)?> PorTokenAsync(string token, CancellationToken ct = default)
    {
        if (ComprobanteService.EmpresaDelToken(token) is not int empresaId) return null;

        var empresa = await _store.PorIdAsync(empresaId, ct);
        if (empresa is null || !empresa.Activa) return null;

        var contexto = new TenantContext();
        contexto.Establecer(empresa, _store.CadenaConexion(empresa));

        await using var db = _factory.Crear(empresa);

        // Se reutiliza la misma lógica de comprobantes, apuntando a la base de esa empresa.
        var servicio = new ComprobanteService(db, _pdf, _email, contexto);
        return await servicio.PorTokenAsync(token);
    }
}
