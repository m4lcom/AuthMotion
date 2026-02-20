using AuthMotion.Application.DTOs;

namespace AuthMotion.Application.Interfaces;

public interface IAuthService
{
    Task<string> RegisterAsync(RegisterRequest request);
}