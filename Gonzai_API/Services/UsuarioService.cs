using Gonzai_API.Data;
using Gonzai_API.DTOs.Usuario;
using Gonzai_API.Models;
using Gonzai_API.Models.Plataforma;
using Gonzai_API.Services.Interfaces;
using Gonzai_API.Services.Tenancy;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class UsuarioService : IUsuarioService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly PlataformaDbContext _catalogo;
    private readonly ITenantContext _tenant;

    public UsuarioService(
        AppDbContext context, IMapper mapper, PlataformaDbContext catalogo, ITenantContext tenant)
    {
        _context  = context;
        _mapper   = mapper;
        _catalogo = catalogo;
        _tenant   = tenant;
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
        var email = dto.Email.Trim().ToLowerInvariant();

        // Un correo pertenece a una sola empresa, así que la unicidad se comprueba en el
        // catálogo de la plataforma y no solo dentro de esta base.
        if (await _catalogo.UsuariosDirectorio.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException($"El email '{email}' ya está registrado.");

        var entrada = new UsuarioDirectorio { Email = email, EmpresaId = _tenant.EmpresaIdRequerido };
        _catalogo.UsuariosDirectorio.Add(entrada);
        await _catalogo.SaveChangesAsync();

        try
        {
            var usuario = _mapper.Map<Usuario>(dto);
            usuario.Email        = email;
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return _mapper.Map<UsuarioResponseDto>(usuario);
        }
        catch
        {
            // Si el usuario no llegó a crearse, el correo no puede quedar reservado.
            _catalogo.UsuariosDirectorio.Remove(entrada);
            await _catalogo.SaveChangesAsync();
            throw;
        }
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
}
