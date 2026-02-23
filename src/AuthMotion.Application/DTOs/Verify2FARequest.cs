namespace AuthMotion.Application.DTOs;

public sealed record Verify2FARequest(string Email, string Code);