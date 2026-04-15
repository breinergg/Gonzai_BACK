using Gonzai_API.Data;
using Gonzai_API.DTOs.PreguntaNoReconocida;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class PreguntaNoReconocidaService : IPreguntaNoReconocidaService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public PreguntaNoReconocidaService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PreguntaNoReconocidaResponseDto>> GetAllAsync()
    {
        var preguntas = await _context.PreguntasNoReconocidas
            .AsNoTracking()
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();

        return _mapper.Map<IEnumerable<PreguntaNoReconocidaResponseDto>>(preguntas);
    }

    public async Task<PreguntaNoReconocidaResponseDto> CreateAsync(PreguntaNoReconocidaCreateDto dto)
    {
        var pregunta = _mapper.Map<PreguntaNoReconocida>(dto);

        _context.PreguntasNoReconocidas.Add(pregunta);
        await _context.SaveChangesAsync();

        return _mapper.Map<PreguntaNoReconocidaResponseDto>(pregunta);
    }
}
