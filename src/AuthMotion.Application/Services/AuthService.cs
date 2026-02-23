using AuthMotion.Application.DTOs;
using AuthMotion.Application.Interfaces;
using AuthMotion.Domain.Entities;
using AuthMotion.Application.Exceptions;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AuthMotion.Application.Services;

// 1. CONSTRUCTOR PRIMARIO: Inyectamos todo directamente en la declaración de la clase.
public class AuthService(
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IEmailService emailService,
    ITwoFactorService twoFactorService) : IAuthService
{
    // Constants for magic numbers
    private const int VerificationTokenExpiryMinutes = 15;
    private const int RefreshTokenExpiryDays = 7;

    public async Task<string> RegisterAsync(RegisterRequest request)
    {
        // Notá que ya no usamos "_userRepository", usamos "userRepository" directo del constructor
        if (await userRepository.IsRegisteredAsync(request.Email))
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

        await userRepository.AddAsync(user);

        var emailBody = GetRegistrationEmailTemplate(otpCode);
        await emailService.SendEmailAsync(user.Email, "Verify your account - AuthMotion", emailBody);

        return "User registered successfully, please check your email for the verification code.";
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid credentials.");
        }

        if (!user.IsEmailVerified)
        {
            throw new UnauthorizedException("Please verify your email before logging in.");
        }

        if (user.IsTwoFactorEnabled)
        {
            return new AuthResponse(null, null, true, "Two-factor authentication required.");
        }

        return await GenerateAndSaveTokensAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(TokenRequest request)
    {
        var principal = jwtTokenGenerator.GetPrincipalFromExpiredToken(request.Token);
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
            throw new UnauthorizedException("Invalid token claims.");

        var user = await userRepository.GetByEmailAsync(email);

        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        return await GenerateAndSaveTokensAsync(user);
    }

    public async Task<AuthResponse> ExternalLoginAsync(string email)
    {
        var user = await userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            user = new User
            {
                Email = email,
                // TODO (Mejora futura): Agregar columna 'IsExternal' o 'AuthProvider' a la DB 
                // para permitir que PasswordHash sea NULL en cuentas de Google/GitHub.
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)))
            };

            await userRepository.AddAsync(user);
        }

        return await GenerateAndSaveTokensAsync(user);
    }

    public async Task<string> VerifyEmailAsync(VerifyEmailRequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.Email);

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

        await userRepository.UpdateAsync(user);

        return "Email verified successfully. You can now log in.";
    }

    public async Task<string> SetupTwoFactorAsync(string email)
    {
        var user = await userRepository.GetByEmailAsync(email);

        if (user == null)
            throw new UnauthorizedException("User not found.");

        var secret = twoFactorService.GenerateSecretKey();

        user.TwoFactorSecret = secret;
        await userRepository.UpdateAsync(user);

        return twoFactorService.GenerateQrCodeUri(user.Email, secret);
    }

    public async Task<bool> ConfirmTwoFactorAsync(string email, string code)
    {
        var user = await userRepository.GetByEmailAsync(email);

        if (user?.TwoFactorSecret == null)
        {
            throw new UnauthorizedException("2FA setup not initiated.");
        }

        var isValid = twoFactorService.ValidateCode(user.TwoFactorSecret, code);

        if (isValid)
        {
            user.IsTwoFactorEnabled = true;
            await userRepository.UpdateAsync(user);
        }

        return isValid;
    }

    public async Task<AuthResponse> Verify2FALoginAsync(Verify2FARequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.Email);

        if (user?.TwoFactorSecret == null)
            throw new UnauthorizedException("User or 2FA setup not found.");

        var isValid = twoFactorService.ValidateCode(user.TwoFactorSecret, request.Code);

        if (!isValid)
            throw new UnauthorizedException("Invalid 2FA code.");

        return await GenerateAndSaveTokensAsync(user);
    }

    public async Task<string> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.Email);

        if (user == null)
        {
            return "If the email is registered, a password reset code has been sent.";
        }

        var resetToken = GenerateSecureOtp();

        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiryTime = DateTime.UtcNow.AddMinutes(VerificationTokenExpiryMinutes);

        await userRepository.UpdateAsync(user);

        var emailBody = GetPasswordResetEmailTemplate(resetToken);
        await emailService.SendEmailAsync(user.Email, "Reset your password - AuthMotion", emailBody);

        return "If the email is registered, a password reset code has been sent.";
    }

    public async Task<string> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.Email);

        if (user == null || user.PasswordResetToken != request.Token || user.PasswordResetTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid or expired password reset token.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiryTime = null;

        await userRepository.UpdateAsync(user);

        return "Password has been reset successfully. You can now log in with your new password.";
    }

    // --- PRIVATE HELPER METHODS ---

    private async Task<AuthResponse> GenerateAndSaveTokensAsync(User user)
    {
        var token = jwtTokenGenerator.GenerateToken(user);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

        await userRepository.UpdateAsync(user);

        return new AuthResponse(token, refreshToken);
    }

    private static string GenerateSecureOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }

    // 2. RAW STRING LITERALS: Código HTML limpio y sin concatenaciones extrañas.
    private static string GetRegistrationEmailTemplate(string otpCode)
    {
        return $"""
            <h2>Welcome to AuthMotion</h2>
            <p>Your 6-digit verification code is:</p>
            <h1 style="color: #2563eb; letter-spacing: 5px;">{otpCode}</h1>
            <p>If you did not request this registration, you can safely ignore this email.</p>
            """;
    }

    private static string GetPasswordResetEmailTemplate(string resetToken)
    {
        return $"""
            <h2>Password Recovery</h2>
            <p>Your password reset code is:</p>
            <h1 style="color: #ef4444; letter-spacing: 5px;">{resetToken}</h1>
            <p>This code expires in 15 minutes. If you did not request a password change, ignore this email and your account will remain secure.</p>
            """;
    }
}