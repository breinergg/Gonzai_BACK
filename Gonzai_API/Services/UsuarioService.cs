using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Gonzai_API.Data;
using Gonzai_API.DTOs.Usuario;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Gonzai_API.Services;

public class UsuarioService : IUsuarioService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public UsuarioService(AppDbContext context, IMapper mapper, IConfiguration configuration)
    {
        _context = context;
        _mapper = mapper;
        _configuration = configuration;
    }

    public async Task<IEnumerable<UsuarioResponseDto>> GetAllAsync()
    {
        var usuarios = await _context.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Nombre)
            .ToListAsync();

        return _mapper.Map<IEnumerable<UsuarioResponseDto>>(usuarios);
    }

    public async Task<UsuarioResponseDto?> GetByIdAsync(int id)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario is null) return null;

        return _mapper.Map<UsuarioResponseDto>(usuario);
    }

    public async Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto dto)
    {
        var emailEnUso = await _context.Usuarios
            .AnyAsync(u => u.Email == dto.Email);

        if (emailEnUso)
            throw new InvalidOperationException($"El email '{dto.Email}' ya está registrado.");

        var usuario = _mapper.Map<Usuario>(dto);
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return _mapper.Map<UsuarioResponseDto>(usuario);
    }

    public async Task<UsuarioResponseDto?> UpdateAsync(int id, UsuarioUpdateDto dto)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario is null) return null;

        _mapper.Map(dto, usuario);
        await _context.SaveChangesAsync();

        return _mapper.Map<UsuarioResponseDto>(usuario);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario is null) return false;

        // Soft delete: preserva ChatLogs y VentasDiarias asociadas
        usuario.Activo = false;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<TokenResponseDto?> LoginAsync(LoginDto dto)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Activo);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
            return null;

        var token = GenerarToken(usuario);
        var usuarioDto = _mapper.Map<UsuarioResponseDto>(usuario);

        return new TokenResponseDto { Token = token, Usuario = usuarioDto };
    }

    public async Task<bool> CambiarPasswordAsync(int id, CambiarPasswordDto dto)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null) return false;

        if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
            throw new InvalidOperationException("La contraseña actual es incorrecta.");

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
        await _context.SaveChangesAsync();

        return true;
    }

    private string GenerarToken(Usuario usuario)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol),
            new Claim(ClaimTypes.Name, usuario.Nombre)
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
