using Gonzai_API.Services.Comprobantes;
using Gonzai_API.Services.Email;
using Gonzai_API.Services.Interfaces;
using Gonzai_API.Services.Tenancy;
using Microsoft.AspNetCore.DataProtection;

namespace Gonzai_API.Services;

public class ComprobantePublicoService : IComprobantePublicoService
{
    private readonly ITenantStore _store;
    private readonly ITenantDbFactory _factory;
    private readonly ComprobantePdfService _pdf;
    private readonly IConfiguration _config;
    private readonly IDataProtectionProvider _dataProtection;

    public ComprobantePublicoService(
        ITenantStore store, ITenantDbFactory factory, ComprobantePdfService pdf,
        IConfiguration config, IDataProtectionProvider dataProtection)
    {
        _store          = store;
        _factory        = factory;
        _pdf            = pdf;
        _config         = config;
        _dataProtection = dataProtection;
    }

    public async Task<(byte[] Pdf, string NombreArchivo)?> PorTokenAsync(string token, CancellationToken ct = default)
    {
        if (ComprobanteService.EmpresaDelToken(token) is not int empresaId) return null;

        var empresa = await _store.PorIdAsync(empresaId, ct);
        if (empresa is null || !empresa.Activa) return null;

        var contexto = new TenantContext();
        contexto.Establecer(empresa, _store.CadenaConexion(empresa));

        await using var db = _factory.Crear(empresa);

        // El IEmailSender normal (inyectado por DI) depende de INegocioService, que a su vez
        // depende del AppDbContext del scope de ESTA petición — pero esta ruta es pública (sin
        // token JWT), así que ese scope no tiene ninguna empresa asignada y su construcción
        // fallaría. Se arma uno manual sobre "db" (la base ya resuelta del token), igual que con
        // ComprobanteService. En la práctica nunca llega a usarse: esta ruta solo descarga el PDF.
        var negocioDelToken = new NegocioService(db, _dataProtection);
        var emailDelToken   = new SmtpEmailSender(_config, negocioDelToken);

        // Se reutiliza la misma lógica de comprobantes, apuntando a la base de esa empresa.
        var servicio = new ComprobanteService(db, _pdf, emailDelToken, contexto);
        return await servicio.PorTokenAsync(token);
    }
}
