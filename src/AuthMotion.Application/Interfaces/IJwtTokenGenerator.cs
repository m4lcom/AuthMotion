using System.Security.Claims;
using AuthMotion.Domain.Entities;

namespace AuthMotion.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}