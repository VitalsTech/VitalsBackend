namespace AuthService.Domain.Entities;

public class EsiaLink
{
    public Guid AuthUserId { get; set; }
    public string EsiaSubjectId { get; set; } = string.Empty;
    public string? Snils { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

    public AuthUser AuthUser { get; set; } = null!;
}
