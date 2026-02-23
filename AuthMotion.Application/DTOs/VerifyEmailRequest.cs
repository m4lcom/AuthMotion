namespace AuthMotion.Application.DTOs;

public sealed record VerifyEmailRequest(string Email, string Code);