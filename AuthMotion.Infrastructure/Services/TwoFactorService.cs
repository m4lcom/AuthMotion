using AuthMotion.Application.Interfaces;
using OtpNet;

namespace AuthMotion.Infrastructure.Services;

public class TwoFactorService : ITwoFactorService
{
    private const string Issuer = "AuthMotion";

    public string GenerateSecretKey()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string GenerateQrCodeUri(string email, string secretKey)
    {
        // URL Encoding is critical here to prevent broken QR codes
        var escapedIssuer = Uri.EscapeDataString(Issuer);
        var escapedEmail = Uri.EscapeDataString(email);

        return $"otpauth://totp/{escapedIssuer}:{escapedEmail}?secret={secretKey}&issuer={escapedIssuer}";
    }

    public bool ValidateCode(string secretKey, string code)
    {
        var key = Base32Encoding.ToBytes(secretKey);
        var totp = new Totp(key);

        // Window of 1 previous and 1 future code handles slight clock drifts on the user's phone
        return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
    }
}