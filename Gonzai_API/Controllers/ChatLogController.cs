using Gonzai_API.DTOs.ChatLog;
using Gonzai_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gonzai_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatLogController : ControllerBase
{
    private readonly IChatLogService _chatLogService;
    private readonly IAIService _aiService;

    public ChatLogController(IChatLogService chatLogService, IAIService aiService)
    {
        _chatLogService = chatLogService;
        _aiService = aiService;
    }

    // GET: api/chatlog
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<ChatLogResponseDto>>> GetAll()
    {
        var logs = await _chatLogService.GetAllAsync();
        return Ok(logs);
    }

    // GET: api/chatlog/usuario/5
    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<ActionResult<IEnumerable<ChatLogResponseDto>>> GetByUsuario(int usuarioId)
    {
        var logs = await _chatLogService.GetByUsuarioIdAsync(usuarioId);
        return Ok(logs);
    }

    // GET: api/chatlog/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ChatLogResponseDto>> GetById(int id)
    {
        var log = await _chatLogService.GetByIdAsync(id);

        if (log is null)
            return NotFound(new { message = $"ChatLog con Id {id} no encontrado." });

        return Ok(log);
    }

    // POST: api/chatlog
    [HttpPost]
    public async Task<ActionResult<ChatLogResponseDto>> Create([FromBody] ChatLogCreateDto dto)
    {
        var resultado = await _aiService.GenerarRespuestaAsync(new AiChatRequest(dto.Pregunta));
        var log = await _chatLogService.CreateAsync(dto, resultado.Respuesta);

        return CreatedAtAction(nameof(GetById), new { id = log.Id }, log);
    }
}
