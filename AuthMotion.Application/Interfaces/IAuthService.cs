using AuthMotion.Application.DTOs;

namespace AuthMotion.Application.Interfaces;

public interface IAuthService
{
    Task<string> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(TokenRequest request);
    Task<AuthResponse> ExternalLoginAsync(string email);
    Task<string> VerifyEmailAsync(VerifyEmailRequest request);
}