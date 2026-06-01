namespace AuthService.Domain.Entities;

public class AuthUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserPublicId { get; set; }
    public string NormalizedPhone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public EsiaLink? EsiaLink { get; set; }
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}
