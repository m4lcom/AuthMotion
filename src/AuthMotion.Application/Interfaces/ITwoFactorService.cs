namespace AuthMotion.Application.Interfaces;

public interface ITwoFactorService
{
    string GenerateSecretKey();
    string GenerateQrCodeUri(string email, string secretKey);
    bool ValidateCode(string secretKey, string code);
}