namespace AuthMotion.Domain.Entities;   
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // verified email
        public bool IsEmailVerified { get; set; }
        public string? EmailVerificationToken { get; set; }
        // 2FA security
        public bool IsTwoFactorEnabled { get; set; }
        public string? TwoFactorSecret { get; set; }
        // refresh token
        public string? RefreshToken { get; set; } 
        public DateTime? RefreshTokenExpiresAt { get; set; }

    }
