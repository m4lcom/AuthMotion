using AuthMotion.Application.DTOs;
using AuthMotion.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;

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

    /// <summary>
    /// register a new user in the system.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(new { message = result });
    }

    /// <summary>
    /// authenticate a user and returns tokens.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// retrieves the current authenticate user's information form claims.
    /// </summary>
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

    /// <summary>
    /// generate a new pair of tokens using a valid refresh token.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// redirect the user to the google sign-in page
    /// </summary>
    [HttpGet("login-google")]
    public IActionResult LoginGoogle()
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// handles the callback from google and issues authmotion tokens
    /// </summary>
    [HttpGet("google-response")]
    public async Task<IActionResult> GoogleResponse()
    {
        // 1. Leemos la identidad temporal que guardó el middleware en la cookie
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded)
            return BadRequest(new { error = "Google authentication failed." });

        // 2. Extraemos el Email de los claims que nos mandó Google
        var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
            return BadRequest(new { error = "Email not found in Google account." });

        // 3. Delegamos la lógica pesada al servicio (crear usuario, generar JWT, etc.)
        // Nota: Este método todavía no existe en IAuthService, lo creamos en el próximo paso.
        var authResponse = await _authService.ExternalLoginAsync(email);

        // 4. Destruimos la cookie temporal porque ya tenemos nuestros propios tokens JWT
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Ok(authResponse);
    }
}