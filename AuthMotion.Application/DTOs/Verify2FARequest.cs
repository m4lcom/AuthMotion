namespace AuthMotion.Application.DTOs;

public record Verify2FARequest(
    string Email,
    string Code
)
{
}