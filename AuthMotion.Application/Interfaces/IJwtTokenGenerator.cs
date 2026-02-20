using AuthMotion.Domain.Entities;

namespace AuthMotion.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}