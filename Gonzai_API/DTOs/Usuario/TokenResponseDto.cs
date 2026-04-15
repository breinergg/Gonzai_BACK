namespace Gonzai_API.DTOs.Usuario;

public class TokenResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UsuarioResponseDto Usuario { get; set; } = null!;
}
