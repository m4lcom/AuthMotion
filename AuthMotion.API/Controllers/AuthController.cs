using AuthMotion.Application.DTOs;
using AuthMotion.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AuthMotion.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(new { message = result });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(new { token = result });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetMe()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(new
        {
            message = "User identity retrieved successfully",
            data = new
            {
                Email = email,
                Id = userId
            }
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return Ok(new { token = result });
    }
}