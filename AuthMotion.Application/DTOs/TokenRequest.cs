namespace AuthMotion.Application.DTOs;

public sealed record TokenRequest(string Token, string RefreshToken);