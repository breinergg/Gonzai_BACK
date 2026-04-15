using Gonzai_API.Data;
using Gonzai_API.DTOs.ChatLog;
using Gonzai_API.Models;
using Gonzai_API.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Gonzai_API.Services;

public class ChatLogService : IChatLogService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public ChatLogService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ChatLogResponseDto>> GetAllAsync()
    {
        var logs = await _context.ChatLogs
            .AsNoTracking()
            .OrderByDescending(c => c.Fecha)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ChatLogResponseDto>>(logs);
    }

    public async Task<IEnumerable<ChatLogResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var logs = await _context.ChatLogs
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.Fecha)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ChatLogResponseDto>>(logs);
    }

    public async Task<ChatLogResponseDto?> GetByIdAsync(int id)
    {
        var log = await _context.ChatLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (log is null) return null;

        return _mapper.Map<ChatLogResponseDto>(log);
    }

    public async Task<ChatLogResponseDto> CreateAsync(ChatLogCreateDto dto, string respuesta)
    {
        var log = _mapper.Map<ChatLog>(dto);
        log.Respuesta = respuesta;

        _context.ChatLogs.Add(log);
        await _context.SaveChangesAsync();

        return _mapper.Map<ChatLogResponseDto>(log);
    }
}
