using System.ComponentModel.DataAnnotations;

namespace AuthMotion.Application.DTOs;

public class LoginRequest
{
    [Required]
    [StringLength(200, ErrorMessage = "Email can't exceed 200 characters.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;
    [Required]
    [StringLength(200, ErrorMessage = "Password can't exceed 200 characters.")]
    public string Password { get; set; } = string.Empty;
}