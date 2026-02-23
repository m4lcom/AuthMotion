using System.ComponentModel.DataAnnotations;

namespace AuthMotion.Application.DTOs;

public sealed record RegisterRequest
{
    [Required]
    [StringLength(200, ErrorMessage = "Email can't exceed 200 characters.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    [StringLength(200, ErrorMessage = "Password can't exceed 200 characters.")]
    public string Password { get; init; } = string.Empty;
}