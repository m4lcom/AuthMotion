namespace AuthMotion.Application.DTOs;

public class AuthResponse
{
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public bool RequiresTwoFactor { get; set; } = false;
    public string? Message { get; set; }
}