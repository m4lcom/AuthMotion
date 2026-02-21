using AuthMotion.Application.Interfaces;
using OtpNet;
using QRCoder;

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
        return $"otpauth://totp/{Issuer}:{email}?secret={secretKey}&issuer={Issuer}";
    }

    public bool ValidateCode(string secretKey, string code)
    {
        var key = Base32Encoding.ToBytes(secretKey);
        var totp = new Totp(key);
        return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
    }
}