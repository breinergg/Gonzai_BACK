using Gonzai_API.Data;
using Gonzai_API.DTOs.VentaDiaria;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class VentaDiariaService : IVentaDiariaService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public VentaDiariaService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<VentaDiariaResponseDto>> GetAllAsync()
    {
        var ventas = await _context.VentasDiarias
            .AsNoTracking()
            .Include(v => v.Usuario)
            .OrderByDescending(v => v.Fecha)
            .ToListAsync();

        return _mapper.Map<IEnumerable<VentaDiariaResponseDto>>(ventas);
    }

    public async Task<IEnumerable<VentaDiariaResponseDto>> GetByFechaAsync(DateOnly fecha)
    {
        var ventas = await _context.VentasDiarias
            .AsNoTracking()
            .Include(v => v.Usuario)
            .Where(v => v.Fecha == fecha)
            .ToListAsync();

        return _mapper.Map<IEnumerable<VentaDiariaResponseDto>>(ventas);
    }

    public async Task<IEnumerable<VentaDiariaResponseDto>> GetByRangoFechaAsync(DateOnly desde, DateOnly hasta)
    {
        var ventas = await _context.VentasDiarias
            .AsNoTracking()
            .Include(v => v.Usuario)
            .Where(v => v.Fecha >= desde && v.Fecha <= hasta)
            .OrderByDescending(v => v.Fecha)
            .ToListAsync();

        return _mapper.Map<IEnumerable<VentaDiariaResponseDto>>(ventas);
    }

    public async Task<VentaDiariaResponseDto?> GetByIdAsync(int id)
    {
        var venta = await _context.VentasDiarias
            .AsNoTracking()
            .Include(v => v.Usuario)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venta is null) return null;

        return _mapper.Map<VentaDiariaResponseDto>(venta);
    }

    public async Task<VentaDiariaResponseDto> CreateAsync(VentaDiariaCreateDto dto)
    {
        if (dto.UsuarioId.HasValue)
        {
            var usuarioExiste = await _context.Usuarios
                .AnyAsync(u => u.Id == dto.UsuarioId.Value && u.Activo);

            if (!usuarioExiste)
                throw new KeyNotFoundException($"No se encontró un usuario activo con Id {dto.UsuarioId}.");
        }

        var venta = _mapper.Map<VentaDiaria>(dto);

        _context.VentasDiarias.Add(venta);
        await _context.SaveChangesAsync();

        await _context.Entry(venta)
            .Reference(v => v.Usuario)
            .LoadAsync();

        return _mapper.Map<VentaDiariaResponseDto>(venta);
    }

    public async Task<VentaDiariaResponseDto?> UpdateAsync(int id, VentaDiariaUpdateDto dto)
    {
        var venta = await _context.VentasDiarias
            .Include(v => v.Usuario)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venta is null) return null;

        if (dto.UsuarioId.HasValue)
        {
            var usuarioExiste = await _context.Usuarios
                .AnyAsync(u => u.Id == dto.UsuarioId.Value && u.Activo);

            if (!usuarioExiste)
                throw new KeyNotFoundException($"No se encontró un usuario activo con Id {dto.UsuarioId}.");
        }

        _mapper.Map(dto, venta);
        await _context.SaveChangesAsync();

        return _mapper.Map<VentaDiariaResponseDto>(venta);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var venta = await _context.VentasDiarias.FindAsync(id);

        if (venta is null) return false;

        _context.VentasDiarias.Remove(venta);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<VentaMensualResumenDto> GetResumenMensualAsync()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        var inicioMesActual = new DateOnly(hoy.Year, hoy.Month, 1);
        var inicioMesAnterior = inicioMesActual.AddMonths(-1);
        var finMesAnterior = inicioMesActual.AddDays(-1);

        var totalActual = await _context.VentasDiarias
            .AsNoTracking()
            .Where(v => v.Fecha >= inicioMesActual && v.Fecha <= hoy)
            .SumAsync(v => (decimal?)v.Total) ?? 0m;

        var totalAnterior = await _context.VentasDiarias
            .AsNoTracking()
            .Where(v => v.Fecha >= inicioMesAnterior && v.Fecha <= finMesAnterior)
            .SumAsync(v => (decimal?)v.Total) ?? 0m;

        decimal porcentajeCambio = 0m;
        if (totalAnterior != 0)
            porcentajeCambio = Math.Round(((totalActual - totalAnterior) / totalAnterior) * 100, 2);

        var cultura = new System.Globalization.CultureInfo("es-ES");

        return new VentaMensualResumenDto
        {
            Mes = hoy.ToString("MMM yyyy", cultura),
            MesAnterior = inicioMesAnterior.ToString("MMM yyyy", cultura),
            TotalMesActual = totalActual,
            TotalMesAnterior = totalAnterior,
            PorcentajeCambio = Math.Abs(porcentajeCambio),
            EsAumento = porcentajeCambio >= 0
        };
    }
}
