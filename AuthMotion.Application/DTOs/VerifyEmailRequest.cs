namespace AuthMotion.Application.DTOs;

public record VerifyEmailRequest(string Email, string Code)
{
    public string Email { get; init; } = Email;
    public string Code { get; init; } = Code;
}