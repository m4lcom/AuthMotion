namespace AuthMotion.Application.DTOs;

public sealed record AuthResponse(string? Token, string? RefreshToken, bool RequiresTwoFactor = false, string? Message = null);