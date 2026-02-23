using AuthMotion.Application.DTOs;
using AuthMotion.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthMotion.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, IWebHostEnvironment env)
    {
        _authService = authService;
        _env = env;
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
    /// authenticate a user and returns tokens via HttpOnly cookies.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        // If 2FA is required, we stop here and do not issue cookies
        if (result.RequiresTwoFactor)
        {
            return Ok(new { requiresTwoFactor = true, message = result.Message });
        }

        SetTokenCookies(result.Token!, result.RefreshToken!);
        return Ok(new { message = "Login successful" });
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
    /// generate a new pair of tokens reading the old ones from HttpOnly cookies.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        // CRITICAL: The frontend no longer sends this in the body. We read it from cookies.
        var token = Request.Cookies["jwt"];
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { message = "Tokens are missing in cookies." });
        }

        // Build the request for the service
        var request = new TokenRequest(token, refreshToken);

        var result = await _authService.RefreshTokenAsync(request);

        SetTokenCookies(result.Token!, result.RefreshToken!);
        return Ok(new { message = "Tokens refreshed successfully" });
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
    /// handles the callback from google and issues authmotion tokens via cookies
    /// </summary>
    [HttpGet("google-response")]
    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded)
            return BadRequest(new { error = "Google authentication failed." });

        var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
            return BadRequest(new { error = "Email not found in Google account." });

        var authResponse = await _authService.ExternalLoginAsync(email);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        SetTokenCookies(authResponse.Token!, authResponse.RefreshToken!);
        return Ok(new { message = "Google login successful" });
    }

    /// <summary>
    /// Test endpoint restricted to users with the "Admin" role.
    /// </summary>
    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnlyEndpoint()
    {
        return Ok(new { message = "Welcome to the VIP area. You have Admin privileges." });
    }

    /// <summary>
    /// Verifies the user's email using the 6-digit OTP code.
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var result = await _authService.VerifyEmailAsync(request);
        return Ok(new { message = result });
    }

    /// <summary>
    /// Initiates the 2FA setup process by generating a QR code URI for the user's authenticator app.
    /// </summary>
    [Authorize]
    [HttpPost("setup-2fa")]
    public async Task<IActionResult> SetupTwoFactor()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var qrUri = await _authService.SetupTwoFactorAsync(email);

        return Ok(new { qrUri });
    }

    /// <summary>
    /// Confirms the 2FA setup by validating the code from the user's authenticator app and activates 2FA on their account.
    /// </summary>
    [Authorize]
    [HttpPost("confirm-2fa")]
    public async Task<IActionResult> ConfirmTwoFactor([FromBody] Confirm2FARequest request)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var success = await _authService.ConfirmTwoFactorAsync(email, request.Code);

        if (!success) return BadRequest("Invalid code.");

        return Ok(new { message = "2FA activated successfully." });
    }

    /// <summary>
    /// Handles the second step of the login process when 2FA is enabled. Validates the 2FA code and issues tokens if successful.
    /// </summary>
    [HttpPost("login-2fa")]
    public async Task<IActionResult> Login2FA([FromBody] Verify2FARequest request)
    {
        var response = await _authService.Verify2FALoginAsync(request);

        SetTokenCookies(response.Token!, response.RefreshToken!);
        return Ok(new { message = "2FA login successful" });
    }

    /// <summary>
    /// Sends a password recovery email with a 6-digit token. 
    /// Protected by Rate Limiting to prevent email spam.
    /// </summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("PasswordRecovery")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        return Ok(new { message = result });
    }

    /// <summary>
    /// Validates the token and resets the user's password.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        return Ok(new { message = result });
    }

    /// <summary>
    /// Clears the authentication cookies to log the user out.
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("jwt");
        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "Logged out successfully" });
    }

    // --- PRIVATE METHODS ---

    private void SetTokenCookies(string token, string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // JavaScript cannot read it (Prevents XSS)
            Secure = _env.IsProduction(),   // Travels only over HTTPS in production
            SameSite = SameSiteMode.Lax, // Required for Next.js to API communication
            Expires = DateTime.UtcNow.AddMinutes(15) // Matches JWT expiration
        };

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = _env.IsProduction(),
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("jwt", token, cookieOptions);
        Response.Cookies.Append("refreshToken", refreshToken, refreshCookieOptions);
    }
}