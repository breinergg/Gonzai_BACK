using System.Text.RegularExpressions;
using Gonzai_API.Data;
using Gonzai_API.DTOs.Plataforma;
using Gonzai_API.Models;
using Gonzai_API.Models.Plataforma;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gonzai_API.Services.Tenancy;

public partial class TenantProvisioningService : ITenantProvisioningService
{
    private readonly PlataformaDbContext _catalogo;
    private readonly ITenantStore _store;
    private readonly ITenantDbFactory _factory;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        PlataformaDbContext catalogo,
        ITenantStore store,
        ITenantDbFactory factory,
        ILogger<TenantProvisioningService> logger)
    {
        _catalogo = catalogo;
        _store    = store;
        _factory  = factory;
        _logger   = logger;
    }

    public async Task<EmpresaResponseDto> CrearAsync(EmpresaCreateDto dto, CancellationToken ct = default)
    {
        var slug  = Normalizar(string.IsNullOrWhiteSpace(dto.Slug) ? dto.Nombre : dto.Slug);
        var email = dto.AdminEmail.Trim().ToLowerInvariant();

        if (slug.Length == 0)
            throw new ArgumentException("No se pudo derivar un identificador válido del nombre de la empresa.");

        if (await _catalogo.Empresas.AnyAsync(e => e.Slug == slug, ct))
            throw new InvalidOperationException($"Ya existe una empresa con el identificador '{slug}'.");

        if (await _catalogo.UsuariosDirectorio.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException($"El correo '{email}' ya está registrado en otra empresa.");

        var empresa = new Empresa
        {
            Nombre             = dto.Nombre.Trim(),
            Slug               = slug,
            BaseDatos          = _store.NombreBaseDatos(slug),
            LimiteChatsDiarios = dto.LimiteChatsDiarios,
            Activa             = true
        };

        // Primero se reserva en el catálogo: los índices únicos impiden que dos altas
        // simultáneas se queden con el mismo identificador o el mismo correo.
        _catalogo.Empresas.Add(empresa);
        _catalogo.UsuariosDirectorio.Add(new UsuarioDirectorio { Email = email, Empresa = empresa });
        await _catalogo.SaveChangesAsync(ct);

        try
        {
            await CrearBaseDatosAsync(empresa.BaseDatos, ct);
            await PrepararBaseAsync(empresa, dto, email, ct);
        }
        catch
        {
            // Si algo falla a medias, no se deja una empresa inservible en el catálogo.
            _catalogo.Empresas.Remove(empresa);
            await _catalogo.SaveChangesAsync(CancellationToken.None);
            await EliminarBaseDatosAsync(empresa.BaseDatos);
            throw;
        }

        _logger.LogInformation("Empresa {Nombre} creada en la base {BaseDatos}.", empresa.Nombre, empresa.BaseDatos);
        return Map(empresa);
    }

    public async Task<EmpresaResponseDto> AdoptarAsync(
        string nombre, string slug, string baseDatos, CancellationToken ct = default)
    {
        var identificador = Normalizar(slug);

        if (await _catalogo.Empresas.AnyAsync(e => e.Slug == identificador || e.BaseDatos == baseDatos, ct))
            throw new InvalidOperationException($"La empresa '{identificador}' ya está registrada.");

        var empresa = new Empresa
        {
            Nombre    = nombre.Trim(),
            Slug      = identificador,
            BaseDatos = Validar(baseDatos),
            Activa    = true
        };

        _catalogo.Empresas.Add(empresa);
        await _catalogo.SaveChangesAsync(ct);

        await using var db = _factory.Crear(empresa);
        await db.Database.MigrateAsync(ct);

        var correos = await db.Usuarios.AsNoTracking().Select(u => u.Email).ToListAsync(ct);

        foreach (var correo in correos)
        {
            var email = correo.Trim().ToLowerInvariant();
            if (await _catalogo.UsuariosDirectorio.AnyAsync(u => u.Email == email, ct)) continue;

            _catalogo.UsuariosDirectorio.Add(new UsuarioDirectorio { Email = email, EmpresaId = empresa.Id });
        }

        await _catalogo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Empresa {Nombre} adoptada sobre la base {BaseDatos} con {Usuarios} usuario(s).",
            empresa.Nombre, empresa.BaseDatos, correos.Count);

        return Map(empresa);
    }

    public async Task<IReadOnlyList<EmpresaResponseDto>> ListarAsync(CancellationToken ct = default) =>
        (await _catalogo.Empresas.AsNoTracking().OrderBy(e => e.Id).ToListAsync(ct)).Select(Map).ToList();

    public async Task<EmpresaResponseDto?> CambiarEstadoAsync(int empresaId, bool activa, CancellationToken ct = default)
    {
        var empresa = await _catalogo.Empresas.FirstOrDefaultAsync(e => e.Id == empresaId, ct);
        if (empresa is null) return null;

        empresa.Activa             = activa;
        empresa.FechaActualizacion = DateTime.UtcNow;
        await _catalogo.SaveChangesAsync(ct);
        _store.OlvidarCache(empresaId);

        return Map(empresa);
    }

    public async Task<int> MigrarTodasAsync(CancellationToken ct = default)
    {
        var empresas = await _store.ActivasAsync(ct);

        foreach (var empresa in empresas)
        {
            await using var db = _factory.Crear(empresa);
            var pendientes = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();

            if (pendientes.Count == 0) continue;

            _logger.LogInformation("Aplicando {Cantidad} migración(es) a {Empresa}.", pendientes.Count, empresa.Nombre);
            await db.Database.MigrateAsync(ct);
        }

        return empresas.Count;
    }

    private async Task PrepararBaseAsync(Empresa empresa, EmpresaCreateDto dto, string email, CancellationToken ct)
    {
        await using var db = _factory.Crear(empresa);
        await db.Database.MigrateAsync(ct);

        db.Usuarios.Add(new Usuario
        {
            Nombre       = dto.AdminNombre.Trim(),
            Email        = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.AdminPassword),
            Rol          = "Admin",
            Activo       = true
        });

        // Los comprobantes salen con el nombre de la empresa desde el primer día.
        db.Negocios.Add(new Negocio { NombreComercial = empresa.Nombre });

        await db.SaveChangesAsync(ct);
    }

    private async Task CrearBaseDatosAsync(string nombre, CancellationToken ct)
    {
        await using var conexion = new NpgsqlConnection(_store.CadenaMantenimiento());
        await conexion.OpenAsync(ct);

        // CREATE DATABASE no admite parámetros, por eso el nombre se valida antes de llegar aquí.
        await using var comando = new NpgsqlCommand($"CREATE DATABASE \"{Validar(nombre)}\"", conexion);
        await comando.ExecuteNonQueryAsync(ct);
    }

    private async Task EliminarBaseDatosAsync(string nombre)
    {
        try
        {
            await using var conexion = new NpgsqlConnection(_store.CadenaMantenimiento());
            await conexion.OpenAsync();
            await using var comando = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{Validar(nombre)}\" WITH (FORCE)", conexion);
            await comando.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo eliminar la base {BaseDatos} tras un alta fallida.", nombre);
        }
    }

    private static string Validar(string nombreBase) =>
        PatronBaseDatos().IsMatch(nombreBase)
            ? nombreBase
            : throw new InvalidOperationException($"Nombre de base de datos no permitido: '{nombreBase}'.");

    private static string Normalizar(string texto)
    {
        var limpio = PatronNoPermitido().Replace(texto.Trim().ToLowerInvariant(), "_").Trim('_');
        return limpio.Length > 40 ? limpio[..40].Trim('_') : limpio;
    }

    private static EmpresaResponseDto Map(Empresa e) => new()
    {
        Id                 = e.Id,
        Nombre             = e.Nombre,
        Slug               = e.Slug,
        BaseDatos          = e.BaseDatos,
        Activa             = e.Activa,
        LimiteChatsDiarios = e.LimiteChatsDiarios,
        FechaCreacion      = e.FechaCreacion
    };

    [GeneratedRegex("^[a-z0-9_]{1,63}$")]
    private static partial Regex PatronBaseDatos();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex PatronNoPermitido();
}
