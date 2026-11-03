using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Gonzai_API.DTOs.Usuario;
using Gonzai_API.Middleware;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using Gonzai_API.Services.Tenancy;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Gonzai_API.Services;

public class AutenticacionService : IAutenticacionService
{
    private readonly ITenantStore _store;
    private readonly ITenantDbFactory _factory;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public AutenticacionService(
        ITenantStore store, ITenantDbFactory factory, IMapper mapper, IConfiguration configuration)
    {
        _store         = store;
        _factory       = factory;
        _mapper        = mapper;
        _configuration = configuration;
    }

    public async Task<TokenResponseDto?> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        // El correo dice a qué empresa pertenece la persona, y por tanto en qué base está su contraseña.
        var empresa = await _store.PorEmailDeUsuarioAsync(email, ct);
        if (empresa is null) return null;

        if (!empresa.Activa)
            throw new InvalidOperationException(
                "La cuenta de tu empresa está inactiva. Comunícate con el administrador de GonzAI.");

        await using var db = _factory.Crear(empresa);

        // La comparación ignora mayúsculas: una base adoptada de un respaldo puede traer
        // correos escritos como "Admin@Tienda.com" y su dueño debe poder entrar igual.
        var usuario = await db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Activo, ct);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
            return null;

        return new TokenResponseDto
        {
            Token   = GenerarToken(usuario, empresa.Id),
            Usuario = _mapper.Map<UsuarioResponseDto>(usuario)
        };
    }

    private string GenerarToken(Usuario usuario, int empresaId)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            // La empresa viaja firmada en el token: nadie puede cambiarla para leer otra base.
            new Claim(TenantMiddleware.ClaimEmpresa, empresaId.ToString())
        };

        var expiracion = DateTime.UtcNow.AddMinutes(
            _configuration.GetValue<int>("Jwt:ExpiresInMinutes"));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiracion,
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
