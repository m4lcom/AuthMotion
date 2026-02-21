namespace AuthMotion.Application.DTOs;

public class Confirm2FARequest(string Code)
{
    public string Code { get; set; } = Code;
}