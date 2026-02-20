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

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid credentials.");
        }

        var token = _jwtTokenGenerator.GenerateToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _userRepository.UpdateAsync(user);

        return new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(TokenRequest request)
    {
        var principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(request.Token);
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        var newToken = _jwtTokenGenerator.GenerateToken(user);
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _userRepository.UpdateAsync(user);

        return new AuthResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken
        };
    }
}