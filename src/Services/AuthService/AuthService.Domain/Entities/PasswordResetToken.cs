namespace AuthService.Domain.Entities;

public class PasswordResetToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuthUserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AuthUser AuthUser { get; set; } = null!;
}
