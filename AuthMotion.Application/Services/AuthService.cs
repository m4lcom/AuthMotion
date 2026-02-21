using AuthMotion.Application.DTOs;
using AuthMotion.Application.Interfaces;
using AuthMotion.Domain.Entities;
using AuthMotion.Application.Exceptions;
using System.Security.Claims;

namespace AuthMotion.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    /// <summary>
    /// Handles user registration and password hashing.
    /// </summary>
    public async Task<string> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.IsRegisteredAsync(request.Email))
        {
            throw new ConflictException("Email is already registered.");
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _userRepository.AddAsync(user);
        return "User registered successfully.";
    }

    /// <summary>
    /// Validates credentials and generates the initial token pair.
    /// </summary>
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid credentials.");
        }

        return await GenerateAndSaveTokensAsync(user);
    }

    /// <summary>
    /// Rotates the refresh token and issues a new access token.
    /// </summary>
    public async Task<AuthResponse> RefreshTokenAsync(TokenRequest request)
    {
        var principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(request.Token);
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email)) throw new UnauthorizedException("Invalid token claims.");

        var user = await _userRepository.GetByEmailAsync(email);

        // Validation: User must exist, tokens must match, and refresh token must not be expired
        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        return await GenerateAndSaveTokensAsync(user);
    }

    /// <summary>
    /// Handles external login (like Google) by generating tokens for the user.
    /// </summary>
    public async Task<AuthResponse> ExternalLoginAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            user = new User
            {
                Email = email,
                // Generate a random hash to prevent breaking BCrypt in traditional login.
                // This forces the user to always use the external provider.
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
            };

            await _userRepository.AddAsync(user);
        }

        return await GenerateAndSaveTokensAsync(user);
    }

    /// <summary>
    /// Centralizes the generation and persistence of the authentication tokens.
    /// </summary>
    private async Task<AuthResponse> GenerateAndSaveTokensAsync(User user)
    {
        var token = _jwtTokenGenerator.GenerateToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _userRepository.UpdateAsync(user);

        return new AuthResponse { Token = token, RefreshToken = refreshToken };
    }
}