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
    private readonly IEmailService _emailService;

    public AuthService(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator, IEmailService emailService)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailService = emailService;
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

        var otpCode = new Random().Next(100000, 999999).ToString();

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsEmailVerified = false,
            VerificationToken = otpCode,
            VerificationTokenExpiryTime = DateTime.UtcNow.AddMinutes(15)
        };

        await _userRepository.AddAsync(user);

        var emailBody = $@"
            <h2>Bienvenido a AuthMotion</h2>
            <p>Tu código de verificación de 6 dígitos es:</p>
            <h1 style='color: #2563eb; letter-spacing: 5px;'>{otpCode}</h1>
            <p>Si no solicitaste este registro, podés ignorar este correo.</p>";

        await _emailService.SendEmailAsync(user.Email, "Verifica tu cuenta - AuthMotion", emailBody);

        return "User registered successfully, please check your email for the verification code.";
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

        if (!user.IsEmailVerified)
        {
            throw new UnauthorizedException("Please verify your email before logging in.");
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

    public async Task<string> VerifyEmailAsync(VerifyEmailRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null) throw new UnauthorizedException("User not found.");

        if (user.IsEmailVerified) return "Email is already verified.";

        if (user.VerificationToken != request.Code || user.VerificationTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid or expired verification code.");
        }

        user.IsEmailVerified = true;
        user.VerificationToken = null;
        user.VerificationTokenExpiryTime = null;

        await _userRepository.UpdateAsync(user);

        return "Email verified successfully. You can now log in.";
    }
}