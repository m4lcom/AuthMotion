using AuthMotion.Application.DTOs;
using AuthMotion.Application.Interfaces;
using AuthMotion.Domain.Entities;
using AuthMotion.Application.Exceptions;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AuthMotion.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailService _emailService;
    private readonly ITwoFactorService _twoFactorService;

    // Constants for magic numbers to improve maintainability
    private const int VerificationTokenExpiryMinutes = 15;
    private const int RefreshTokenExpiryDays = 7;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IEmailService emailService,
        ITwoFactorService twoFactorService)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailService = emailService;
        _twoFactorService = twoFactorService;
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

        var otpCode = GenerateSecureOtp();

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsEmailVerified = false,
            VerificationToken = otpCode,
            VerificationTokenExpiryTime = DateTime.UtcNow.AddMinutes(VerificationTokenExpiryMinutes)
        };

        await _userRepository.AddAsync(user);

        var emailBody = GetRegistrationEmailTemplate(otpCode);
        await _emailService.SendEmailAsync(user.Email, "Verify your account - AuthMotion", emailBody);

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

        // Busca esta parte:
        if (user.IsTwoFactorEnabled)
        {
            return new AuthResponse(null, null, true, "Two-factor authentication required.");
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

        if (string.IsNullOrEmpty(email))
            throw new UnauthorizedException("Invalid token claims.");

        var user = await _userRepository.GetByEmailAsync(email);

        // Validation: User must exist, tokens must match, and refresh token must be valid
        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        return await GenerateAndSaveTokensAsync(user);
    }

    /// <summary>
    /// Handles external login (e.g., Google) by generating tokens for the user.
    /// </summary>
    public async Task<AuthResponse> ExternalLoginAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            user = new User
            {
                Email = email,
                // Generate a random secure hash to prevent traditional login bypass
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)))
            };

            await _userRepository.AddAsync(user);
        }

        return await GenerateAndSaveTokensAsync(user);
    }

    public async Task<string> VerifyEmailAsync(VerifyEmailRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null)
            throw new UnauthorizedException("User not found.");

        if (user.IsEmailVerified)
            return "Email is already verified.";

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

    public async Task<string> SetupTwoFactorAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
            throw new UnauthorizedException("User not found.");

        var secret = _twoFactorService.GenerateSecretKey();

        // Save secret but do NOT enable IsTwoFactorEnabled yet
        user.TwoFactorSecret = secret;
        await _userRepository.UpdateAsync(user);

        return _twoFactorService.GenerateQrCodeUri(user.Email, secret);
    }

    public async Task<bool> ConfirmTwoFactorAsync(string email, string code)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user?.TwoFactorSecret == null)
        {
            throw new UnauthorizedException("2FA setup not initiated.");
        }

        var isValid = _twoFactorService.ValidateCode(user.TwoFactorSecret, code);

        if (isValid)
        {
            user.IsTwoFactorEnabled = true;
            await _userRepository.UpdateAsync(user);
        }

        return isValid;
    }

    public async Task<AuthResponse> Verify2FALoginAsync(Verify2FARequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user?.TwoFactorSecret == null)
            throw new UnauthorizedException("User or 2FA setup not found.");

        var isValid = _twoFactorService.ValidateCode(user.TwoFactorSecret, request.Code);

        if (!isValid)
            throw new UnauthorizedException("Invalid 2FA code.");

        return await GenerateAndSaveTokensAsync(user);
    }

    public async Task<string> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        // Golden rule: Do not reveal if the email exists. Fail silently.
        if (user == null)
        {
            return "If the email is registered, a password reset code has been sent.";
        }

        var resetToken = GenerateSecureOtp();

        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiryTime = DateTime.UtcNow.AddMinutes(VerificationTokenExpiryMinutes);

        await _userRepository.UpdateAsync(user);

        var emailBody = GetPasswordResetEmailTemplate(resetToken);
        await _emailService.SendEmailAsync(user.Email, "Reset your password - AuthMotion", emailBody);

        return "If the email is registered, a password reset code has been sent.";
    }

    public async Task<string> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null || user.PasswordResetToken != request.Token || user.PasswordResetTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid or expired password reset token.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiryTime = null;

        await _userRepository.UpdateAsync(user);

        return "Password has been reset successfully. You can now log in with your new password.";
    }

    // --- PRIVATE HELPER METHODS ---

    /// <summary>
    /// Centralizes the generation and persistence of the authentication tokens.
    /// </summary>
    private async Task<AuthResponse> GenerateAndSaveTokensAsync(User user)
    {
        var token = _jwtTokenGenerator.GenerateToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

        await _userRepository.UpdateAsync(user);

        return new AuthResponse(token, refreshToken);
    }

    /// <summary>
    /// Generates a cryptographically secure 6-digit OTP.
    /// </summary>
    private static string GenerateSecureOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }

    private static string GetRegistrationEmailTemplate(string otpCode)
    {
        return $@"
            <h2>Welcome to AuthMotion</h2>
            <p>Your 6-digit verification code is:</p>
            <h1 style='color: #2563eb; letter-spacing: 5px;'>{otpCode}</h1>
            <p>If you did not request this registration, you can safely ignore this email.</p>";
    }

    private static string GetPasswordResetEmailTemplate(string resetToken)
    {
        return $@"
            <h2>Password Recovery</h2>
            <p>Your password reset code is:</p>
            <h1 style='color: #ef4444; letter-spacing: 5px;'>{resetToken}</h1>
            <p>This code expires in 15 minutes. If you did not request a password change, ignore this email and your account will remain secure.</p>";
    }
}