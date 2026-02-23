using System.ComponentModel.DataAnnotations;

namespace AuthMotion.Application.DTOs;

public sealed record LoginRequest
{
    [Required]
    [StringLength(200, ErrorMessage = "Email can't exceed 200 characters.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(200, ErrorMessage = "Password can't exceed 200 characters.")]
    public string Password { get; init; } = string.Empty;
}