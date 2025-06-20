namespace SharingMezzi.Core.DTOs;


public class AuthResultDto
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UtenteDto? User { get; set; }
}
