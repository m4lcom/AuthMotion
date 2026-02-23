namespace AuthMotion.Application.DTOs;

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);